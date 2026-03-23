(function () {
  const JWT_KEY = 'tcg_jwt';
  const API_BASE = '';

  let neonAuthUrl = '';
  let userId = null;
  let jwt = sessionStorage.getItem(JWT_KEY);
  let profile = null;
  let connection = null;
  let matchId = null;
  let state = null;
  let cardDefs = {};
  let attackingInstanceId = null;

  const $ = id => document.getElementById(id);

  function setAuthStatus(msg) {
    const el = $('authStatus');
    if (el) el.textContent = msg || '';
  }

  function api(path, options = {}) {
    const url = API_BASE + path;
    const headers = { ...options.headers };
    if (!jwt) return Promise.reject(new Error('Not authenticated. Sign in first.'));
    headers['Authorization'] = 'Bearer ' + jwt;
    return fetch(url, { ...options, headers, credentials: 'include' });
  }

  async function neonSignIn(email, password) {
    const res = await fetch(neonAuthUrl + '/sign-in/email', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
      credentials: 'include'
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || data.error || 'Sign in failed');
    return data;
  }

  async function neonSignUp(name, email, password) {
    const res = await fetch(neonAuthUrl + '/sign-up/email', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, email, password }),
      credentials: 'include'
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || data.error || 'Sign up failed');
    return data;
  }

  async function fetchNeonJwt(sessionToken) {
    const headers = sessionToken ? { Authorization: 'Bearer ' + sessionToken } : {};
    const res = await fetch(neonAuthUrl + '/token', { credentials: 'include', headers });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || data.error || 'Failed to get token');
    return data?.token ?? data?.data?.token;
  }

  async function neonSignOut() {
    try {
      await fetch(neonAuthUrl + '/sign-out', { method: 'POST', credentials: 'include' });
    } catch (_) {}
    jwt = null;
    userId = null;
    profile = null;
    sessionStorage.removeItem(JWT_KEY);
  }

  function connectSignalR() {
    if (!userId || !jwt) return;
    const url = '/hubs/game?access_token=' + encodeURIComponent(jwt);
    connection = new signalR.HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect()
      .build();

    connection.on('MatchFound', (mId, opponentDeckId) => {
      matchId = mId;
      $('auth').style.display = 'none';
      $('lobby').style.display = 'none';
      $('deckSection').style.display = 'none';
      $('game').style.display = 'block';
      $('gameOver').style.display = 'none';
      if (state) renderGame();
      else $('gameStatus').textContent = 'Match found! Loading...';
    });

    connection.on('StateUpdate', (newState) => {
      state = newState;
      if (matchId) renderGame();
    });

    connection.on('GameOver', (winnerId) => {
      const won = winnerId === userId;
      $('game').style.display = 'none';
      $('gameOver').style.display = 'block';
      $('gameOverText').textContent = won ? 'You win!' : 'You lose!';
      matchId = null;
      state = null;
    });

    connection.on('Error', (msg) => {
      $('status').textContent = 'Error: ' + msg;
    });

    return connection.start();
  }

  async function ensureDeck() {
    const res = await api('/api/decks');
    const decks = await res.json();
    if (decks.length > 0) return decks;

    const cardsRes = await api('/api/cards/definitions');
    const cards = await cardsRes.json();
    const creatures = cards.filter(c => c.attack != null && c.defense != null);
    if (creatures.length < 2) throw new Error('Not enough creature cards to build deck');

    const slots = [];
    let total = 0;
    for (let i = 0; i < Math.min(8, creatures.length) && total < 30; i++) {
      const qty = Math.min(4, 30 - total);
      slots.push({ cardDefinitionId: creatures[i].id, quantity: qty });
      total += qty;
    }
    while (total < 30) {
      const add = Math.min(4, 30 - total);
      slots.push({ cardDefinitionId: creatures[0].id, quantity: add });
      total += add;
    }

    const createRes = await api('/api/decks', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name: 'Default Deck', slots })
    });
    if (!createRes.ok) {
      const err = await createRes.json().catch(() => ({}));
      throw new Error(err.error || err.message || 'Failed to create deck');
    }
    const created = await createRes.json();
    return [created];
  }

  function getMyPlayer() {
    if (!state || !userId) return null;
    return state.players[userId] || null;
  }

  function getOpponent() {
    if (!state) return null;
    return Object.values(state.players).find(p => p.userId !== userId) || null;
  }

  function getCardName(cardDefId) {
    const id = typeof cardDefId === 'string' ? cardDefId : cardDefId;
    const def = cardDefs[id];
    return def ? def.name : '?';
  }

  function renderGame() {
    const me = getMyPlayer();
    const opp = getOpponent();
    if (!me || !opp) return;

    const myTurn = state.currentPlayerId === userId;

    $('myLife').textContent = me.lifeTotal;
    $('oppLife').textContent = opp.lifeTotal;

    $('gameStatus').textContent = myTurn ? 'Your turn' : "Opponent's turn";

    $('endTurn').disabled = !myTurn;

    const myHand = $('myHand');
    myHand.innerHTML = '';
    for (const instId of me.hand || []) {
      const info = state.cardInstances?.[instId];
      if (!info || (info.cardType !== 'Creature' && info.cardType !== 0)) continue;
      const card = document.createElement('div');
      card.className = 'card' + (myTurn ? '' : ' disabled');
      card.innerHTML = `<span>${getCardName(info.cardDefinitionId)}</span><span class="stats">${info.attack}/${info.defense}</span>`;
      if (myTurn) card.onclick = () => playCard(instId);
      myHand.appendChild(card);
    }

    const myBoard = $('myBoard');
    myBoard.innerHTML = '';
    for (const c of me.board || []) {
      const card = document.createElement('div');
      card.dataset.instance = c.instanceId;
      card.className = 'card creature-on-board' + (myTurn && c.playedOnTurn < state.currentTurn ? '' : ' disabled');
      card.innerHTML = `<span>${getCardName(c.cardDefinitionId)}</span><span class="stats">${c.currentAttack}/${c.currentDefense}</span>`;
      if (myTurn && c.playedOnTurn < state.currentTurn) {
        card.onclick = () => {
          if (attackingInstanceId === c.instanceId) {
            attackingInstanceId = null;
            $('gameStatus').textContent = 'Your turn';
            renderGame();
          } else if (attackingInstanceId) {
            selectAttackTarget(c.instanceId);
          } else {
            attackingInstanceId = c.instanceId;
            $('gameStatus').textContent = 'Select target (opponent creature or Attack Face)';
            renderGame();
          }
        };
      }
      myBoard.appendChild(card);
    }

    const oppBoard = $('oppBoard');
    oppBoard.innerHTML = '';
    for (const c of opp.board || []) {
      const card = document.createElement('div');
      card.className = 'card creature-on-board' + (attackingInstanceId && myTurn ? '' : ' disabled');
      card.innerHTML = `<span>${getCardName(c.cardDefinitionId)}</span><span class="stats">${c.currentAttack}/${c.currentDefense}</span>`;
      if (attackingInstanceId && myTurn) card.onclick = () => selectAttackTarget(c.instanceId);
      oppBoard.appendChild(card);
    }

    const attackFaceBtn = $('attackFace');
    attackFaceBtn.style.display = (attackingInstanceId && myTurn) ? 'inline-block' : 'none';
    attackFaceBtn.onclick = () => selectAttackTarget('face');
    attackFaceBtn.textContent = 'Attack Face';

    if (attackingInstanceId) {
      const sel = myBoard.querySelector(`[data-instance="${attackingInstanceId}"]`);
      if (sel) sel.classList.add('selected');
    }
  }

  function selectAttackTarget(targetInstanceId) {
    if (!attackingInstanceId || !matchId) return;
    const tid = targetInstanceId === 'face' ? 'nonexistent-target' : targetInstanceId;
    connection.invoke('Attack', matchId, attackingInstanceId, tid).catch(err => console.error(err));
    attackingInstanceId = null;
  }

  function playCard(instanceId) {
    if (!matchId || !connection) return;
    connection.invoke('PlayCard', matchId, instanceId, null).catch(err => console.error(err));
  }

  function renderProfile() {
    const el = $('profileDisplay');
    if (!profile) { el.innerHTML = ''; return; }
    el.innerHTML = `<strong>${escapeHtml(profile.displayName || userId)}</strong> • Wins: ${profile.wins} • Losses: ${profile.losses}`;
  }

  function escapeHtml(s) {
    const div = document.createElement('div');
    div.textContent = s;
    return div.innerHTML;
  }

  async function loadProfile() {
    try {
      const res = await api('/api/users/me');
      if (res.ok) {
        profile = await res.json();
        renderProfile();
      }
    } catch (_) {}
  }

  async function init() {
    try {
      const cfgRes = await fetch(API_BASE + '/api/auth/config');
      const cfg = await cfgRes.json();
      neonAuthUrl = (cfg.neonAuthUrl || '').replace(/\/$/, '');
      if (!neonAuthUrl) throw new Error('Auth not configured');
    } catch (err) {
      $('status').textContent = 'Error: ' + (err.message || err) + '. Set NEON_AUTH_URL.';
      return;
    }

    if (jwt) {
      try {
        const res = await api('/api/users/me');
        if (res.ok) {
          profile = await res.json();
          userId = profile.id;
          $('auth').style.display = 'none';
          $('lobby').style.display = 'block';
          renderProfile();
          await loadLobbyData();
        } else {
          jwt = null;
          sessionStorage.removeItem(JWT_KEY);
        }
      } catch (_) {
        jwt = null;
        sessionStorage.removeItem(JWT_KEY);
      }
    }
  }

  async function loadLobbyData() {
    $('status').textContent = 'Loading...';
    try {
      const cardsRes = await api('/api/cards/definitions');
      const cards = await cardsRes.json();
      cards.forEach(c => { cardDefs[c.id] = c; });
      const decks = await ensureDeck();
      const deckSelect = $('deckSelect');
      deckSelect.innerHTML = decks.map(d => `<option value="${d.id}">${d.name}</option>`).join('');
      await connectSignalR();
      $('status').textContent = 'Ready. Select a deck and click Find Match.';
    } catch (err) {
      $('status').textContent = 'Error: ' + (err.message || err);
    }
  }

  $('signIn')?.addEventListener('click', async () => {
    const email = $('authEmail')?.value?.trim();
    const password = $('authPassword')?.value;
    if (!email || !password) {
      setAuthStatus('Enter email and password.');
      return;
    }
    setAuthStatus('Signing in...');
    try {
      const data = await neonSignIn(email, password);
      const user = data?.data?.user ?? data?.user;
      const uid = user?.id ?? user?.sub;
      const sessionToken = data?.token ?? data?.data?.session?.token;
      if (!uid) {
        console.error('Neon Auth sign-in response:', data);
        setAuthStatus('Unexpected response from sign-in. Check browser console for details.');
        return;
      }
      let token;
      try {
        token = await fetchNeonJwt(sessionToken);
      } catch (err) {
        setAuthStatus('Error: ' + (err.message || err));
        return;
      }
      if (token) {
        jwt = token;
        userId = uid;
        sessionStorage.setItem(JWT_KEY, jwt);
        setAuthStatus('');
        $('auth').style.display = 'none';
        $('lobby').style.display = 'block';
        await loadProfile();
        await loadLobbyData();
      } else {
        setAuthStatus('Failed to get auth token. Check Neon Auth JWT config.');
      }
    } catch (err) {
      setAuthStatus('Error: ' + (err.message || err));
    }
  });

  $('signUp')?.addEventListener('click', async () => {
    const name = $('signUpName')?.value?.trim() || $('signUpEmail')?.value?.trim();
    const email = $('signUpEmail')?.value?.trim();
    const password = $('signUpPassword')?.value;
    if (!email || !password) {
      setAuthStatus('Enter email and password.');
      return;
    }
    setAuthStatus('Signing up...');
    try {
      const data = await neonSignUp(name || email, email, password);
      const user = data?.data?.user ?? data?.user;
      const uid = user?.id ?? user?.sub;
      const sessionToken = data?.token ?? data?.data?.session?.token;
      if (uid) {
        let token;
        try {
          token = await fetchNeonJwt(sessionToken);
        } catch (_) {
          token = null;
        }
        if (token) {
          jwt = token;
          userId = uid;
          sessionStorage.setItem(JWT_KEY, jwt);
          setAuthStatus('');
          $('auth').style.display = 'none';
          $('lobby').style.display = 'block';
          await loadProfile();
          await loadLobbyData();
          return;
        }
      }
      setAuthStatus('Sign up successful. Please sign in.');
    } catch (err) {
      setAuthStatus('Error: ' + (err.message || err));
    }
  });

  $('signOut')?.addEventListener('click', async () => {
    if (connection) {
      try { await connection.stop(); } catch (_) {}
      connection = null;
    }
    await neonSignOut();
    $('lobby').style.display = 'none';
    $('game').style.display = 'none';
    $('gameOver').style.display = 'none';
    $('auth').style.display = 'block';
    setAuthStatus('');
    $('authEmail').value = '';
    $('authPassword').value = '';
    $('signUpName').value = '';
    $('signUpEmail').value = '';
    $('signUpPassword').value = '';
    $('status').textContent = '';
  });

  $('findMatch')?.addEventListener('click', async () => {
    const deckId = $('deckSelect')?.value;
    if (!deckId || !connection) return;
    $('status').textContent = 'Finding match...';
    $('findMatch').disabled = true;
    try {
      await connection.invoke('JoinQueue', deckId);
      $('status').textContent = 'Waiting for opponent...';
    } catch (err) {
      $('status').textContent = 'Error: ' + (err.message || err);
      $('findMatch').disabled = false;
    }
  });

  $('endTurn')?.addEventListener('click', () => {
    if (attackingInstanceId) attackingInstanceId = null;
    if (!matchId || !connection) return;
    connection.invoke('EndTurn', matchId).catch(err => console.error(err));
  });

  $('backToLobby')?.addEventListener('click', () => {
    $('gameOver').style.display = 'none';
    $('lobby').style.display = 'block';
    $('status').textContent = 'Select a deck and click Find Match.';
    $('findMatch').disabled = false;
  });

  init();
})();
