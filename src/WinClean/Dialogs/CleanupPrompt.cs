namespace WinClean.Dialogs;

/// <summary>The confirmation before a cleanup: what goes, how much, and what the user should know first.</summary>
public sealed record CleanupPrompt(string Question, string Message, IReadOnlyList<string> Lines, IReadOnlyList<string> Notices, string ConfirmLabel);
