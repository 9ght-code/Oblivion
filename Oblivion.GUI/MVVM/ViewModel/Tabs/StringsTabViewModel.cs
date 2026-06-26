using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Oblivion.GUI.Domain.Abstractions;

namespace Oblivion.GUI.MVVM.ViewModel.Tabs;

public partial class StringsTabViewModel : ViewModelBase
{
    private readonly IAnalysisContext _context;

    public ObservableCollection<ExtractedString> ExtractedStrings { get; } = new();

    public StringsTabViewModel(IAnalysisContext context)
    {
        _context = context;
        ExtractStrings();
    }

    private void ExtractStrings()
    {
        var fileBytes = _context.FileBytes;
        if (fileBytes == null) return;

        int minLength = 4;
        var all = new List<ExtractedString>();

        // ASCII pass
        var sb = new StringBuilder();
        long startOffset = 0;

        for (int i = 0; i < fileBytes.Length; i++)
        {
            byte b = fileBytes[i];
            if (b >= 32 && b < 127)
            {
                if (sb.Length == 0) startOffset = i;
                sb.Append((char)b);
            }
            else
            {
                if (sb.Length >= minLength)
                {
                    all.Add(new ExtractedString
                    {
                        Offset = $"0x{startOffset:X}",
                        Value = sb.ToString(),
                        Length = sb.Length,
                        Encoding = "ASCII",
                        OffsetRaw = startOffset
                    });
                }
                sb.Clear();
            }
        }
        if (sb.Length >= minLength)
        {
            all.Add(new ExtractedString
            {
                Offset = $"0x{startOffset:X}",
                Value = sb.ToString(),
                Length = sb.Length,
                Encoding = "ASCII",
                OffsetRaw = startOffset
            });
        }

        // UTF-16LE pass
        var wSb = new StringBuilder();
        long wStartOffset = 0;

        for (int i = 0; i + 1 < fileBytes.Length; i += 2)
        {
            byte lo = fileBytes[i];
            byte hi = fileBytes[i + 1];
            if (lo >= 32 && lo < 127 && hi == 0)
            {
                if (wSb.Length == 0) wStartOffset = i;
                wSb.Append((char)lo);
            }
            else
            {
                if (wSb.Length >= minLength)
                {
                    all.Add(new ExtractedString
                    {
                        Offset = $"0x{wStartOffset:X}",
                        Value = wSb.ToString(),
                        Length = wSb.Length,
                        Encoding = "UTF-16LE",
                        OffsetRaw = wStartOffset
                    });
                }
                wSb.Clear();
                i -= 1;
            }
        }
        if (wSb.Length >= minLength)
        {
            all.Add(new ExtractedString
            {
                Offset = $"0x{wStartOffset:X}",
                Value = wSb.ToString(),
                Length = wSb.Length,
                Encoding = "UTF-16LE",
                OffsetRaw = wStartOffset
            });
        }

        // Sort by file offset, deduplicate overlapping entries
        all.Sort((a, b) => a.OffsetRaw.CompareTo(b.OffsetRaw));

        long lastEnd = -1;
        foreach (var s in all)
        {
            long entryEnd = s.OffsetRaw + (s.Encoding == "UTF-16LE" ? s.Length * 2 : s.Length);
            if (s.OffsetRaw >= lastEnd)
            {
                ExtractedStrings.Add(s);
                lastEnd = entryEnd;
            }
        }
    }
}
