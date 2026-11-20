using System;
using Shouldly;
using Xunit;

namespace LosTresMosqueteros.Pantry;

public class PantryItemTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\n ")]
    public void Blank_note_clears_the_private_note(string? blank)
    {
        var item = new PantryItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        item.UpdatePrivateNote("Primera línea\nSegunda línea");
        item.PrivateNote.ShouldBe("Primera línea\nSegunda línea");

        item.UpdatePrivateNote(blank);

        item.PrivateNote.ShouldBeNull();
    }

    [Fact]
    public void Note_above_the_limit_is_rejected_without_changing_the_current_note()
    {
        var item = new PantryItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var maxNote = new string('x', PantryItemConsts.MaxPrivateNoteLength);
        item.UpdatePrivateNote(maxNote);

        Should.Throw<ArgumentException>(() => item.UpdatePrivateNote(maxNote + "x"));

        item.PrivateNote.ShouldBe(maxNote);
    }
}
