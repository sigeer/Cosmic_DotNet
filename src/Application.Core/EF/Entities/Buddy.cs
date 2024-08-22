﻿namespace Application.EF.Entities;

public partial class Buddy
{
    public Buddy()
    {
    }

    public Buddy(int characterId, int buddyId, sbyte pending, string? group)
    {
        CharacterId = characterId;
        BuddyId = buddyId;
        Pending = pending;
        Group = group;
    }

    public int Id { get; set; }

    public int CharacterId { get; set; }

    public int BuddyId { get; set; }

    public sbyte Pending { get; set; }

    public string? Group { get; set; }
}
