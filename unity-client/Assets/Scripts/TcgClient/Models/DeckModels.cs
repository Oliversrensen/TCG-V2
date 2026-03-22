using System;

namespace TcgClient.Models
{
    [Serializable]
    public class DeckSlotDto
    {
        public string CardDefinitionId;
        public int Quantity;
    }

    [Serializable]
    public class CreateDeckRequest
    {
        public string Name;
        public DeckSlotDto[] Slots;
    }
}
