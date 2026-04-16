# Plan: Refactor Markdown Lowering to Use Markdig AST

## Problem Statement

The `Markdown` view exposes a public `MarkdownPipeline` property that implies users can customize parsing behavior via Markdig's pipeline extensions. In reality, the parsed AST is immediately discarded:

```csharp
// MarkdownView.Parsing.cs:24-29
MarkdownPipeline pipeline = MarkdownPipeline ?? _defaultPipeline;
_ = Markdig.Markdown.Parse (_markdown, pipeline);   // AST discarded
LowerFromSourceText ();                              // regex from raw text
```

`LowerFromSourceText()` re-parses the raw markdown string using hand-written regexes. This means:

1. **`MarkdownPipeline` is dead API** - custom pipelines have zero effect on rendering.
2. **CommonMark divergence** - the regex lowerer doesn't handle many valid Markdown constructs (nested emphasis, escaped chars, lazy continuation, setext headings, indented code blocks, link reference definitions, etc.).
3. **Duplicated work** - Markdig already produces a complete, correct AST; the regex parser is a second, less-correct parser.

## Goal

Replace `LowerFromSourceText()` with `LowerFromAst(MarkdownDocument doc)` that walks Markdig's parsed AST and produces the same `List<IntermediateBlock>` output. This makes the `MarkdownPipeline` property meaningful and gets rendering closer to spec-compliant CommonMark/GFM.

---

## Current Architecture (as-is)

```
Raw Markdown string
      |
      v
EnsureParsed()
      |
      +---> Markdig.Markdown.Parse() --> AST (discarded)
      |
      +---> LowerFromSourceText()  [regex-based, line-by-line]
                |
                v
          List<IntermediateBlock>   (each with List<InlineRun>)
                |
                v
          BuildRenderedLines()     [word-wrap, SubView creation]
                |
                +---> RenderedLine[]  (text blocks)
                +---> MarkdownCodeBlock SubViews  (contiguous code lines)
                +---> MarkdownTable SubViews  (from TableData)
                +---> Line SubViews  (thematic breaks)
                |
                v
          OnDrawingSubViews() / OnDrawingContent()
```

### Key Data Structures

- **`IntermediateBlock`** - parsing output: `(InlineRun[] runs, bool wrap, string prefix, string continuationPrefix, bool isCodeBlock, string? anchor, bool isThematicBreak, TableData? tableData)`
- **`InlineRun`** - inline span: `(string text, MarkdownStyleRole role, string? url, string? imageSource, Attribute? attribute)`
- **`RenderedLine`** - layout output: `(StyledSegment[] segments, bool wrapEligible, int width, bool isCodeBlock, bool isThematicBreak, bool isTable)`

### What the regex parser handles today

| Markdown construct | Regex pattern | IntermediateBlock output |
|---|---|---|
| Headings `# ...` | `^(#{1,6})\s+(.+)$` | `runs` with HeadingMarker + Heading inlines, `anchor` slug |
| Unordered lists `- ...` | `^\s*[-*+]\s+(.*)$` | `prefix="* "`, inlines parsed |
| Ordered lists `1. ...` | `^\s*\d+\.\s+(.*)$` | `prefix="1. "`, inlines parsed |
| Task lists `- [x] ...` | `^\[(?<state>[ xX])\]\s+(.*)$` | TaskDone/TaskTodo role |
| Fenced code blocks | `` ``` `` fence detection | `isCodeBlock=true`, syntax-highlighted runs |
| Block quotes `> ...` | `line.TrimStart().StartsWith('>')` | `prefix="> "` |
| Thematic breaks | `---`, `***`, `___` (3+ chars) | `isThematicBreak=true` |
| Tables `\| ... \|` | Pipe-delimited line accumulation | `tableData` from `TableData.TryParse()` |
| Paragraphs | Everything else | `wrap=true`, inlines parsed |
| Inline: bold, italic, code, links, images | `MarkdownInlineParser` (character-scan) | InlineRun with appropriate role |

---

## Proposed Architecture (to-be)

```
Raw Markdown string
      |
      v
EnsureParsed()
      |
      v
Markdig.Markdown.Parse(text, pipeline) --> MarkdownDocument AST
      |
      v
LowerFromAst(MarkdownDocument)   [AST walker]
      |
      v
List<IntermediateBlock>   (same shape as before)
      |
      v
BuildRenderedLines()   (UNCHANGED)
      ... rest of pipeline UNCHANGED ...
```

### Principle: Change only the lowering; keep everything downstream

The `IntermediateBlock` / `RenderedLine` / SubView pipeline is well-designed and working. The refactor replaces **only** the code that produces `List<IntermediateBlock>` from the source. Everything from `BuildRenderedLines()` onward stays the same.

---

## Testing Strategy Overview

Tests are organized in three categories that run at specific points in the plan:

1. **Pre-work coverage tests** — Fill gaps in existing test coverage *before* any refactoring. These establish a behavioral baseline: if the refactor breaks something, these tests catch it.

2. **Pre-work should-fail tests** — Tests that document known limitations of the regex parser. They are written to *expect correct behavior*, run to confirm they fail, then attributed with `[Fact (Skip = "Requires AST-based lowering")]`. They become the acceptance criteria for the refactor.

3. **New impl tests** — Unit tests written alongside each implementation phase, testing the new/modified functions directly.

All tests go in `Tests/UnitTestsParallelizable/Views/Markdown/`.

---

## Phase 0: Pre-Work Testing (Baseline Coverage)

### Phase 0a: Coverage Tests for Existing Behavior

These tests document what the current regex parser *does* handle correctly. They must all pass before any refactoring begins.

**Blockquote coverage** (currently only 1 test: `Style_Quote_Marker_Bold_Text_Faint`):

```
BlockQuote_Single_Line_Renders_With_Prefix
  Input: "> Hello world"
  Assert: rendered line has prefix "> " with Quote role, "Hello world" with Quote role

BlockQuote_With_Bold_Inline
  Input: "> This is **important**"
  Assert: prefix "> ", "This is " Quote role, "important" Strong role

BlockQuote_With_Link
  Input: "> See [docs](https://example.com)"
  Assert: prefix "> ", "See " Quote role, "docs" Link role with URL

BlockQuote_Multiple_Consecutive_Lines
  Input: "> Line one\n> Line two"
  Assert: 2 rendered blocks, each with "> " prefix

BlockQuote_Empty_Quote_Line
  Input: ">\n> After blank"
  Assert: first block is empty with prefix, second has content

BlockQuote_WordWrap_Respects_Prefix
  Input: "> This is a long line that should wrap at the viewport boundary"
  Viewport: 30 columns
  Assert: continuation lines have "> " prefix (continuationPrefix)
```

**List coverage gaps:**

```
OrderedList_Multiple_Items
  Input: "1. First\n2. Second\n3. Third"
  Assert: 3 blocks with appropriate prefixes

UnorderedList_Nested_Not_Supported_Renders_Flat
  Input: "- Parent\n  - Child"
  Assert: both render (current behavior, even if nesting is lost)

TaskList_Mixed_States
  Input: "- [x] Done\n- [ ] Todo\n- [X] Also done"
  Assert: correct TaskDone/TaskTodo roles for each
```

**Code block coverage gaps:**

```
CodeBlock_Empty_Fence
  Input: "```\n```"
  Assert: empty code block is created

CodeBlock_With_Language_And_Highlighting
  Input: "```csharp\nvar x = 1;\n```"
  Assert: code block has language "csharp", lines are present

CodeBlock_Multiple_Blocks_Create_Separate_SubViews
  Input: "```\nA\n```\n\nText\n\n```\nB\n```"
  Assert: 2 MarkdownCodeBlock SubViews
```

**Heading coverage gaps:**

```
Heading_All_Levels_1_Through_6
  Input: "# H1\n## H2\n### H3\n#### H4\n##### H5\n###### H6"
  Assert: 6 heading blocks with correct anchor slugs

Heading_With_Inline_Formatting
  Input: "# Title with **bold** and *italic*"
  Assert: heading block contains Strong and Emphasis runs
```

**Thematic break coverage:**

```
ThematicBreak_Dashes_Creates_Line_SubView
  Input: "---"
  Assert: Line SubView created

ThematicBreak_Stars_Creates_Line_SubView
  Input: "***"
  Assert: Line SubView created

ThematicBreak_Underscores_Creates_Line_SubView
  Input: "___"
  Assert: Line SubView created
```

### Phase 0b: Should-Fail Tests (Proving the Regex Parser is Broken)

These tests document constructs that *should* work per CommonMark but don't with the regex parser. Write them, run them, confirm failure, then add `Skip`.

**Escaped characters:**

```
[Fact (Skip = "Requires AST-based lowering")]
Escaped_Asterisks_Not_Treated_As_Emphasis
  Input: "This is \\*not bold\\*"
  Assert: entire line renders as Normal role, no Emphasis
  Current: regex parser treats \* as emphasis delimiter

[Fact (Skip = "Requires AST-based lowering")]
Escaped_Backtick_Not_Treated_As_Code
  Input: "Use \\`backticks\\` literally"
  Assert: renders as plain text
```

**Nested emphasis:**

```
[Fact (Skip = "Requires AST-based lowering")]
Triple_Asterisks_Bold_Italic
  Input: "This is ***bold italic***"
  Assert: content has both Strong and Emphasis roles applied
  Current: regex parser can't nest emphasis

[Fact (Skip = "Requires AST-based lowering")]
Bold_Inside_Italic
  Input: "*italic and **bold** inside*"
  Assert: outer text is Emphasis, inner "bold" is Strong
```

**Setext headings:**

```
[Fact (Skip = "Requires AST-based lowering")]
Setext_Heading_Level_1
  Input: "Title\n====="
  Assert: renders as Heading block with anchor slug
  Current: regex parser only handles ATX headings (# prefix)

[Fact (Skip = "Requires AST-based lowering")]
Setext_Heading_Level_2
  Input: "Subtitle\n--------"
  Assert: renders as Heading block
  Current: "--------" is treated as a thematic break
```

**Indented code blocks:**

```
[Fact (Skip = "Requires AST-based lowering")]
Indented_Code_Block
  Input: "Paragraph\n\n    code line 1\n    code line 2\n\nAfter"
  Assert: middle lines render as code block
  Current: regex parser only detects fenced (```) code blocks
```

**Custom pipeline effects:**

```
[Fact (Skip = "Requires AST-based lowering")]
Custom_Pipeline_Without_Tables_Renders_Pipes_As_Text
  Input: "| A | B |\n|---|---|\n| 1 | 2 |"
  Pipeline: new MarkdownPipelineBuilder().Build()  (no table extension)
  Assert: pipes render as plain paragraph text, NOT as a table SubView
  Current: pipeline property has no effect

[Fact (Skip = "Requires AST-based lowering")]
Custom_Pipeline_With_Footnotes_Produces_Footnote_Content
  Input: "Text[^1]\n\n[^1]: Footnote content"
  Pipeline: new MarkdownPipelineBuilder().UseFootnotes().Build()
  Assert: footnote reference and content both render (even as plain text)
```

**Nested blockquotes:**

```
[Fact (Skip = "Requires AST-based lowering")]
Nested_BlockQuote_Has_Double_Prefix
  Input: "> > Nested quote"
  Assert: rendered with prefix "> > " (double-nested)
  Current: regex parser strips first > only, second > appears as text

[Fact (Skip = "Requires AST-based lowering")]
BlockQuote_Containing_List
  Input: "> - Item one\n> - Item two"
  Assert: rendered with prefix "> * " or similar compound prefix
  Current: regex parser treats entire line as quote text (list markers not parsed inside quotes)

[Fact (Skip = "Requires AST-based lowering")]
BlockQuote_Containing_Code_Block
  Input: "> ```\n> code\n> ```"
  Assert: code renders within quote context
  Current: fence detection doesn't account for > prefix
```

**HTML entities:**

```
[Fact (Skip = "Requires AST-based lowering")]
Html_Entity_Renders_As_Character
  Input: "Copyright &copy; 2024"
  Assert: renders as "Copyright (c) 2024" or the actual symbol
  Current: regex parser renders literal "&copy;"
```

**Strikethrough:**

The `DefaultMarkdownSample` advertises `~~strikethrough~~` but `MarkdownInlineParser` has no
`~~` delimiter handling and `MarkdownStyleRole` has no `Strikethrough` value. The text renders
with literal tildes. Markdig's `UseAdvancedExtensions()` already includes the strikethrough
extension — the AST inline walker gets this for free via `EmphasisInline` with
`DelimiterChar == '~'` and `DelimiterCount == 2`. A new `MarkdownStyleRole.Strikethrough`
value and a corresponding `MarkdownAttributeHelper` case (applying `TextStyle.Strikethrough`)
are needed.

```
[Fact (Skip = "Requires AST-based lowering")]
Strikethrough_Renders_With_Strikethrough_Style
  Input: "This is ~~struck~~ text"
  Assert: "struck" has Strikethrough role with Strikethrough text style
  Current: renders as literal "~~struck~~" with Normal role

[Fact (Skip = "Requires AST-based lowering")]
Strikethrough_With_Other_Inline_Formatting
  Input: "**bold** and ~~struck~~ and *italic*"
  Assert: "bold" Strong, "struck" Strikethrough, "italic" Emphasis
  Current: tildes rendered literally

[Fact (Skip = "Requires AST-based lowering")]
Strikethrough_In_DefaultMarkdownSample_Renders_Correctly
  Input: Markdown.DefaultMarkdownSample
  Assert: the word "strikethrough" in the sample has Strikethrough text style
  Current: renders as "~~strikethrough~~" with tildes visible
```

**Autolinks:**

```
[Fact (Skip = "Requires AST-based lowering")]
Autolink_Renders_As_Link
  Input: "<https://example.com>"
  Assert: renders as Link role with URL
  Current: regex parser treats < > as plain text
```

---

## Phase 1: AST Block Walker

Replace `LowerFromSourceText()` with a method that walks `MarkdownDocument` top-level blocks:

```
MarkdownDocument
  +-- HeadingBlock           --> IntermediateBlock(Heading inlines, anchor)
  +-- ParagraphBlock         --> IntermediateBlock(wrap=true, parsed inlines)
  +-- FencedCodeBlock        --> IntermediateBlock(isCodeBlock=true) per line
  +-- CodeBlock              --> IntermediateBlock(isCodeBlock=true) per line (NEW)
  +-- QuoteBlock             --> IntermediateBlock(prefix="> ") recurse children
  +-- ListBlock
  |     +-- ListItemBlock    --> IntermediateBlock(prefix="* " or "1. ")
  |           +-- TaskList   --> TaskDone/TaskTodo roles
  +-- ThematicBreakBlock     --> IntermediateBlock(isThematicBreak=true)
  +-- Table (ext)            --> IntermediateBlock(tableData=...)
  +-- HtmlBlock              --> (v1: render as plain text paragraph)
  +-- LinkReferenceDefinition --> (consumed by inline resolution, not rendered)
```

#### Markdig AST types to handle (Markdig 0.39.0)

**Block-level** (`Markdig.Syntax` namespace):
- `HeadingBlock` - `.Level`, `.Inline` (contains inline elements)
- `ParagraphBlock` - `.Inline`
- `FencedCodeBlock` - `.Info` (language), `.Lines` (StringLineGroup)
- `CodeBlock` - indented code blocks (no fence)
- `QuoteBlock` - contains sub-blocks
- `ListBlock` - `.IsOrdered`, contains `ListItemBlock`s
- `ListItemBlock` - contains sub-blocks (paragraphs, nested lists)
- `ThematicBreakBlock` - no content
- `HtmlBlock` - raw HTML (render as plain text in terminal)
- `LinkReferenceDefinitionGroup` - consumed during inline parsing

**Block-level extensions** (`Markdig.Extensions.Tables` etc.):
- `Table` - `.ColumnDefinitions`, contains `TableRow`s
- `TableRow` - `.IsHeader`, contains `TableCell`s
- `TableCell` - contains inline content

### Phase 1 Tests

```
LowerFromAst_HeadingBlock_Creates_Heading_IntermediateBlock
  Parse "# Hello" through Markdig, call LowerFromAst
  Assert: block has Heading role runs, anchor slug "hello"

LowerFromAst_ParagraphBlock_Creates_Wrappable_Block
  Parse "Hello world" through Markdig
  Assert: block has wrap=true, Normal role

LowerFromAst_ThematicBreakBlock_Creates_ThematicBreak
  Parse "---" through Markdig
  Assert: block has isThematicBreak=true

LowerFromAst_FencedCodeBlock_Creates_CodeBlock_Per_Line
  Parse "```csharp\nline1\nline2\n```"
  Assert: 2 blocks with isCodeBlock=true

LowerFromAst_IndentedCodeBlock_Creates_CodeBlock_Per_Line
  Parse "    indented code"
  Assert: block with isCodeBlock=true (NEW capability)

LowerFromAst_QuoteBlock_Adds_Prefix
  Parse "> Hello"
  Assert: block has prefix="> ", continuation="> "

LowerFromAst_EmptyDocument_Creates_Empty_Block
  Parse ""
  Assert: no blocks (or single empty block per existing behavior)

LowerFromAst_BlankLine_Between_Paragraphs
  Parse "Para 1\n\nPara 2"
  Assert: blank IntermediateBlock between the two paragraphs

LowerFromAst_HtmlBlock_Renders_As_PlainText
  Parse "<div>hello</div>" (preceded by blank line so Markdig treats as HtmlBlock)
  Assert: renders as plain text paragraph, not swallowed
```

---

## Phase 2: AST Inline Walker

Replace `MarkdownInlineParser.ParseInlines()` usage in the main pipeline. Markdig already parsed inline formatting into a linked list of `Inline` objects.

**Pre-requisite:** Add `Strikethrough` to `MarkdownStyleRole` enum and add a case in
`MarkdownAttributeHelper` that applies `TextStyle.Strikethrough`. This is needed for the
inline walker to map `EmphasisInline` with `DelimiterChar == '~'` correctly.

**Inline-level** (`Markdig.Syntax.Inlines` namespace):
- `LiteralInline` - plain text (`.Content` is `StringSlice`)
- `EmphasisInline` - `*` or `**` (`.DelimiterCount` distinguishes); also `~~` strikethrough (`DelimiterChar == '~'`, `DelimiterCount == 2`)
- `CodeInline` - backtick code (`.Content`)
- `LinkInline` - `[text](url)` (`.Url`, `.IsImage`, contains child inlines)
- `AutolinkInline` - `<url>`
- `LineBreakInline` - hard/soft line breaks
- `HtmlInline` - inline HTML (rare in terminal context)
- `HtmlEntityInline` - `&amp;` etc.

```csharp
// Conceptual: walk Markdig's inline linked list
List<InlineRun> WalkInlines (Inline? inline, MarkdownStyleRole defaultRole)
{
    List<InlineRun> runs = [];
    while (inline != null)
    {
        switch (inline)
        {
            case LiteralInline lit:
                runs.Add (new InlineRun (lit.Content.ToString (), defaultRole));
                break;
            case EmphasisInline em:
                MarkdownStyleRole role = em.DelimiterChar == '~'
                    ? MarkdownStyleRole.Strikethrough
                    : em.DelimiterCount >= 2
                        ? MarkdownStyleRole.Strong
                        : MarkdownStyleRole.Emphasis;
                runs.AddRange (WalkInlines (em.FirstChild, role));
                break;
            case CodeInline code:
                runs.Add (new InlineRun (code.Content, MarkdownStyleRole.InlineCode));
                break;
            case LinkInline link:
                if (link.IsImage)
                    runs.Add (new InlineRun (
                        GetFallbackText (link),
                        MarkdownStyleRole.ImageAlt,
                        imageSource: link.Url));
                else
                    runs.AddRange (WalkInlines (link.FirstChild, MarkdownStyleRole.Link)
                        .Select (r => r with { Url = link.Url }));
                break;
            case LineBreakInline:
                runs.Add (new InlineRun (" ", defaultRole));
                break;
            case HtmlEntityInline entity:
                runs.Add (new InlineRun (
                    entity.Transcoded.ToString (), defaultRole));
                break;
            case AutolinkInline auto:
                runs.Add (new InlineRun (
                    auto.Url, MarkdownStyleRole.Link, auto.Url));
                break;
        }
        inline = inline.NextSibling;
    }
    return runs;
}
```

### Phase 2 Tests

```
WalkInlines_LiteralInline_Returns_Normal_Run
  Assert: plain text -> single InlineRun with defaultRole

WalkInlines_EmphasisInline_Single_Returns_Emphasis
  Assert: *text* -> InlineRun with Emphasis role

WalkInlines_EmphasisInline_Double_Returns_Strong
  Assert: **text** -> InlineRun with Strong role

WalkInlines_Strikethrough_Returns_Strikethrough
  Assert: ~~text~~ -> InlineRun with Strikethrough role

WalkInlines_Strikethrough_Mixed_With_Bold_And_Italic
  Assert: **bold** ~~struck~~ *italic* -> Strong, Strikethrough, Emphasis runs

WalkInlines_Nested_Emphasis_Bold_Inside_Italic
  Assert: *italic **bold** italic* -> Emphasis, Strong, Emphasis runs

WalkInlines_CodeInline_Returns_InlineCode
  Assert: `code` -> InlineRun with InlineCode role

WalkInlines_LinkInline_Returns_Link_With_Url
  Assert: [text](url) -> InlineRun with Link role and URL set

WalkInlines_ImageInline_Returns_ImageAlt
  Assert: ![alt](src) -> InlineRun with ImageAlt role and imageSource

WalkInlines_AutolinkInline_Returns_Link
  Assert: <https://example.com> -> InlineRun with Link role (NEW)

WalkInlines_HtmlEntityInline_Returns_Transcoded
  Assert: &amp; -> InlineRun with "&" text (NEW)

WalkInlines_LineBreakInline_Returns_Space
  Assert: soft break -> InlineRun with " " text

WalkInlines_EscapedAsterisks_No_Emphasis
  Assert: \*text\* -> InlineRun with plain text (NEW, previously broken)

WalkInlines_Empty_Returns_Empty_List
  Assert: null inline -> []
```

---

## Phase 3: Table Handling (Dragon #1)

**Current approach:** `TableData.TryParse()` re-parses pipe-delimited lines from raw source text and returns a `TableData(headers, alignments, rows)` where headers/rows are `string[][]`.

**The dragon:** Markdig's `Table` extension produces `Table > TableRow > TableCell` AST nodes where each `TableCell` contains inline elements (already parsed). But `TableData` stores raw strings (which `MarkdownTable` then re-parses with `MarkdownInlineParser.ParseInlines()`). This creates a mismatch:

- **Option A (minimal change):** Extract cell text from AST `TableCell.Inline` as a flat string, feed it into the existing `TableData(string[] headers, ...)` constructor. `MarkdownTable` continues to call `MarkdownInlineParser.ParseInlines()` on cell text during its `Data` setter. This duplicates inline parsing but keeps `MarkdownTable` and `TableData` unchanged.

- **Option B (cleaner):** Change `TableData` to carry `List<InlineRun>[]` per cell instead of `string[]`. Then `MarkdownTable` would skip re-parsing inlines. But this requires modifying `MarkdownTable.Data` setter, `ParseCellSegments()`, and test code.

**Recommendation:** Start with Option A. It's a v1 refactor and we want to change one thing at a time. The double-parse is cheap and keeps the blast radius small. Option B can follow.

**Additional concern:** Markdig's `Table` extension must be enabled in the pipeline for table AST nodes to appear. The current default pipeline uses `UseAdvancedExtensions()` which includes tables, but a user-supplied custom pipeline might not. Need to handle the case where table syntax appears in source but the pipeline doesn't include the table extension (Markdig won't produce `Table` nodes; the pipe-delimited lines will appear inside `ParagraphBlock`s as literal text). This is actually *correct* behavior — if the user's pipeline doesn't enable tables, tables shouldn't render as tables.

### Phase 3 Tests

```
LowerFromAst_Table_Creates_TableData_IntermediateBlock
  Parse "| A | B |\n|---|---|\n| 1 | 2 |"
  Assert: IntermediateBlock with IsTable=true, TableData has 2 columns, 1 row

LowerFromAst_Table_Preserves_Alignment
  Parse "| Left | Center | Right |\n|:---|:---:|---:|\n| a | b | c |"
  Assert: TableData.Alignments = [Start, Center, End]

LowerFromAst_Table_Cell_With_Inline_Formatting
  Parse "| **bold** | *italic* |\n|---|---|\n| `code` | [link](url) |"
  Assert: TableData cell strings contain the raw markdown for re-parsing
  (Option A: strings like "**bold**"; Option B: InlineRun lists)

LowerFromAst_Table_Multiple_Rows
  Parse table with 3 body rows
  Assert: TableData.Rows.Length == 3

LowerFromAst_Pipeline_Without_Tables_Renders_As_Text
  Pipeline: no table extension
  Input: "| A | B |\n|---|---|\n| 1 | 2 |"
  Assert: renders as plain paragraphs, NOT as table SubView
  (This is the KEY test proving MarkdownPipeline works)

MarkdownTable_Standalone_Text_Still_Works_After_Refactor
  MarkdownTable.Text = "| a | b |\n|---|---|\n| 1 | 2 |"
  Assert: table renders correctly (standalone path unchanged)
```

---

## Phase 4: Code Block Handling (Dragon #2)

**Current approach:** `LowerFromSourceText()` detects `` ``` `` fences, accumulates lines, and calls `AddCodeBlockLines()` which:
1. Optionally runs `SyntaxHighlighter.Highlight()` per line
2. Creates one `IntermediateBlock(isCodeBlock=true)` per line
3. Later, `SyncCodeBlockViews()` groups contiguous code lines into `MarkdownCodeBlock` SubViews

**Markdig AST:** `FencedCodeBlock` has `.Info` (language string) and `.Lines` (a `StringLineGroup` — essentially `StringSlice[]`). There's also `CodeBlock` for indented code (no fence, no language).

**The dragon is mild here.** The mapping is straightforward:
- `FencedCodeBlock.Info` -> language for syntax highlighting
- `FencedCodeBlock.Lines[i]` -> each line becomes `IntermediateBlock(isCodeBlock=true)`
- Syntax highlighting still runs the same way on extracted line text
- `SyncCodeBlockViews()` downstream is unchanged

**Indented code blocks** (`CodeBlock` without fence) are a *new* feature gain — the regex parser can't detect them (it only looks for `` ``` ``). The AST walker handles them for free.

### Phase 4 Tests

```
LowerFromAst_FencedCodeBlock_Extracts_Language
  Parse "```python\nprint('hi')\n```"
  Assert: syntax highlighting called with language "python"

LowerFromAst_FencedCodeBlock_Lines_Become_Separate_Blocks
  Parse "```\nline1\nline2\nline3\n```"
  Assert: 3 IntermediateBlocks with isCodeBlock=true

LowerFromAst_FencedCodeBlock_With_SyntaxHighlighter
  Set SyntaxHighlighter, parse code block
  Assert: InlineRuns have Attribute from highlighter (same as current behavior)

LowerFromAst_IndentedCodeBlock_Treated_As_CodeBlock
  Parse "    code line" (4 spaces)
  Assert: IntermediateBlock with isCodeBlock=true (NEW capability)

LowerFromAst_FencedCodeBlock_Empty_Creates_Empty_Block
  Parse "```\n```"
  Assert: empty code block IntermediateBlock

LowerFromAst_CodeBlock_Between_Paragraphs_Creates_Separate_SubView
  Parse "Before\n\n```\ncode\n```\n\nAfter"
  Assert: paragraph, code block, paragraph — 3 distinct sections

MarkdownCodeBlock_Standalone_Text_Still_Works
  MarkdownCodeBlock.Text = "```csharp\nvar x = 1;\n```"
  Assert: standalone path unchanged
```

---

## Phase 5: Nested Blocks (Dragon #3 — the real dragon)

**Current approach:** `LowerFromSourceText()` is a flat, line-by-line loop. There is no concept of nesting. A block quote line `> some text` is a single `IntermediateBlock` with `prefix="> "`. A list item `- some text` is a single `IntermediateBlock` with `prefix="* "`.

**Markdig AST:** Blocks nest. A `QuoteBlock` contains sub-blocks (paragraphs, lists, code blocks, even more quote blocks). A `ListItemBlock` contains sub-blocks. This means:

```markdown
> - item one
>   ```python
>   code here
>   ```
> - item two
```

Produces: `QuoteBlock > ListBlock > ListItemBlock > [ParagraphBlock, FencedCodeBlock]`

**The current `IntermediateBlock` model cannot express this.** Each `IntermediateBlock` is flat — it has a prefix and runs, but no children. The layout pipeline (`BuildRenderedLines`, `WrapBlock`) also assumes flat blocks.

**This is why the original author likely backed away from AST-based lowering.** The gap between Markdig's recursive tree and the flat `IntermediateBlock` list is significant for nested constructs.

#### Pragmatic approach for v1

**Flatten during lowering.** Walk the AST recursively but produce flat `IntermediateBlock`s, accumulating prefixes:

```
QuoteBlock
  ListBlock (unordered)
    ListItemBlock
      ParagraphBlock "item one"
```

Becomes: `IntermediateBlock(runs=["item one"], prefix="> * ", continuationPrefix=">   ")`

This matches what the current regex parser produces for simple cases and extends naturally to deeper nesting. The key insight is that `prefix` and `continuationPrefix` are already string accumulators — we just need to build them recursively:

```csharp
void WalkBlock (Block block, string prefix, string contPrefix)
{
    switch (block)
    {
        case QuoteBlock quote:
            foreach (Block child in quote)
                WalkBlock (child, prefix + "> ", contPrefix + "> ");
            break;
        case ListBlock list:
            foreach (ListItemBlock item in list)
            {
                string marker = list.IsOrdered ? $"{item.Order}. " : "* ";
                WalkBlock (item.FirstOrDefault (), prefix + marker,
                    contPrefix + new string (' ', marker.Length));
                foreach (Block sub in item.Skip (1))
                    WalkBlock (sub, contPrefix + new string (' ', marker.Length),
                        contPrefix + new string (' ', marker.Length));
            }
            break;
        case ParagraphBlock para:
            List<InlineRun> runs = WalkInlines (para.Inline);
            _blocks.Add (new IntermediateBlock (runs, wrap: true, prefix, contPrefix));
            break;
        // ... etc
    }
}
```

**Limitation:** Nested code blocks inside quotes/lists will lose their SubView positioning because the prefix system doesn't account for SubView indentation. For v1, this is acceptable — nested code blocks would render as indented text without the code block background. This matches what many terminal markdown renderers do.

### Phase 5 Tests

```
LowerFromAst_NestedQuote_Has_Double_Prefix
  Input: "> > Nested"
  Assert: prefix="> > ", continuation="> > "

LowerFromAst_QuoteBlock_With_List
  Input: "> - Item one\n> - Item two"
  Assert: two blocks with prefix="> * " (or "> • ")

LowerFromAst_QuoteBlock_With_Multiple_Paragraphs
  Input: "> Para 1\n>\n> Para 2"
  Assert: two paragraph blocks + blank block, all with "> " prefix

LowerFromAst_ListItem_With_Multiple_Paragraphs
  Input: "- First para\n\n  Second para in same item"
  Assert: first block prefix="* ", second block prefix="  " (continuation)

LowerFromAst_Nested_List_In_List
  Input: "- Parent\n  - Child"
  Assert: parent has prefix="* ", child has prefix="  * " or similar

LowerFromAst_QuoteBlock_Containing_CodeBlock
  Input: "> ```\n> code\n> ```"
  Assert: code blocks render with "> " context
  (v1: may render as indented text rather than code SubView — document limitation)

LowerFromAst_Deeply_Nested_Does_Not_Crash
  Input: "> > > > Deep nesting"
  Assert: prefix="> > > > ", no stack overflow

LowerFromAst_OrderedList_Preserves_Numbers
  Input: "1. First\n2. Second\n3. Third"
  Assert: correct numbering in prefix (may differ from regex which hardcodes "1. ")
```

---

## Phase 6: Wiring and Cleanup

1. **Wire `EnsureParsed()`** to call `LowerFromAst(doc)` instead of `LowerFromSourceText()`
2. **Delete `LowerFromSourceText()`** and its regex fields (`_headingPattern`, `_unorderedListPattern`, etc.)
3. **Keep `MarkdownInlineParser`** — still needed for `MarkdownTable` and `MarkdownCodeBlock` standalone paths
4. **Remove the `_ = Markdig.Markdown.Parse(...)` discard** — actually use the return value
5. **Update XML docs** on `MarkdownPipeline` to describe what pipeline options actually affect rendering
6. **Handle unknown AST node types** with fallback rendering (plain text paragraph)

### Phase 6 Tests

```
EnsureParsed_Uses_Ast_Not_Regex
  Set MarkdownPipeline to custom pipeline without tables
  Input with tables: should render as text, not table SubView
  Assert: proves AST path is active (regex path would still make tables)

Pipeline_Property_Change_Invalidates_And_Reparses
  Set text with table, verify table SubView exists
  Change pipeline to one without tables
  Assert: table SubView removed, renders as text

Remove_Skip_From_All_ShouldFail_Tests
  Remove [Skip] from all Phase 0b tests
  Assert: they ALL pass now

MarkdownInlineParser_Still_Works_For_Standalone_Use
  Call MarkdownInlineParser.ParseInlines ("**bold**", MarkdownStyleRole.Normal)
  Assert: returns Strong run (utility is still functional)

DefaultMarkdownSample_Renders_Without_Exceptions
  Set Text = Markdown.DefaultMarkdownSample
  Force layout
  Assert: no exceptions, content size > 0

Unknown_AstNode_Renders_As_PlainText
  Use pipeline extension that produces custom block type
  Assert: renders as plain paragraph (not swallowed)
```

---

## Challenges and Risk Analysis

### High Risk

**1. Nested block flattening fidelity**
- The current renderer has never dealt with nested structures. Flattening `QuoteBlock > ListBlock > ...` into prefix strings may produce unexpected visual results for deeply nested content.
- **Mitigation:** Test with real-world Markdown files (README.md files from popular repos). Accept "good enough" flattening for v1; deeply nested exotic constructs can be addressed incrementally.

**2. Inline parsing behavioral differences**
- Markdig's inline parser handles edge cases the regex parser doesn't: escaped characters (`\*not bold\*`), nested emphasis (`***bold italic***`), autolinks (`<https://...>`), HTML entities (`&amp;`).
- These are **improvements** but they change behavior. Any tests asserting on the regex parser's quirky behavior will need updating.
- **Mitigation:** Audit all existing tests before starting. Document any intentional behavioral changes.

**3. `MarkdownTable` standalone path**
- `MarkdownTable` has a `Text` property setter that parses pipe-delimited text directly via `TableData.TryParse()`. This standalone path doesn't go through the `Markdown` view's pipeline at all.
- `MarkdownTable` also calls `MarkdownInlineParser.ParseInlines()` internally in `ParseCellSegments()`.
- **This means `MarkdownInlineParser` cannot be deleted** — it's still needed for standalone `MarkdownTable` usage and for `MarkdownCodeBlock.Text` path.
- **Mitigation:** Keep `MarkdownInlineParser` but stop using it in the `Markdown` view's main pipeline. Document it as a utility for standalone SubView usage.

### Medium Risk

**4. Pipeline extension coverage**
- Users may supply pipelines with extensions that produce AST nodes the walker doesn't know about (e.g., `FootnoteBlock`, `AbbreviationBlock`, `DefinitionList`, `MathBlock`, custom extensions).
- **Mitigation:** Add a default/fallback case in the AST walker that extracts raw text from unknown block types and renders as a plain paragraph. Log or raise a diagnostic event for unhandled types.

**5. Source position mapping loss**
- The regex parser operates on raw source lines, so error positions map trivially. The AST walker operates on AST nodes whose `.Span` property gives source positions, but the mapping is less direct.
- **Mitigation:** This is only relevant for debugging. Not a user-facing concern.

**6. Performance**
- The regex parser does one pass over lines. The AST walker allocates Markdig's full AST graph, then walks it. For very large Markdown documents this could be slower.
- **Mitigation:** Profile before/after. Markdig is well-optimized; the overhead is likely negligible compared to layout and drawing.

### Low Risk

**7. Setext headings, indented code, link reference definitions**
- These are valid CommonMark constructs the regex parser can't handle. The AST walker gets them for free.
- They are **new features**, not regressions. But they should be tested.

**8. Hard line breaks**
- CommonMark supports hard line breaks via trailing `  ` (two spaces) or `\` at end of line. Markdig produces `LineBreakInline` nodes with `.IsHard`. The regex parser ignores these entirely.
- The AST walker needs to decide: does a hard break within a paragraph produce a new `IntermediateBlock` (new rendered line) or just a newline character? The current `IntermediateBlock` model assumes one block per logical paragraph.
- **Mitigation:** For v1, treat hard breaks as spaces (same as current behavior). Optionally emit a line break by splitting the paragraph into multiple `IntermediateBlock`s.

---

## Why Did the Original Author Back Away from AST Lowering?

Based on the code evidence, my hypothesis:

1. **The comment `"parsed AST is intentionally unused in v1 lowering"` was aspirational.** The parse call was kept as a placeholder for a future where the AST would be used, but the regex approach was faster to prototype.

2. **Nested blocks are hard.** The flat `IntermediateBlock` model was designed for line-by-line processing. Making it work with Markdig's recursive tree requires either (a) changing `IntermediateBlock` to support children, or (b) flattening during lowering. Option (a) would cascade into the entire layout/drawing pipeline. Option (b) is subtle to get right.

3. **Tables were already working.** `TableData.TryParse()` was a self-contained regex-based table parser that produced exactly what `MarkdownTable` needed. Replacing it with AST walking would require restructuring `TableData` or adding a conversion layer. The path of least resistance was to keep the regex parser.

4. **The inline parser was already written.** `MarkdownInlineParser` handles bold, italic, code, links, and images. Replacing it with Markdig inline walking is straightforward but represents throwaway work for the original implementation.

In short: the regex approach was a working prototype, and the gap to AST-based lowering was larger than it appeared because of nesting and table integration. The `MarkdownPipeline` property was likely added speculatively for future use, and the `Parse()` call was kept to validate the pipeline without actually using its output.

---

## Execution Summary

| Step | Description | Files Changed | Risk |
|------|-------------|---------------|------|
| **0a** | Pre-work coverage tests (blockquotes, lists, headings, code, thematic breaks) | `Tests/` | Low |
| **0b** | Pre-work should-fail tests (escapes, nesting, setext, indented code, pipeline) | `Tests/` | Low |
| **1** | AST block walker (headings, paragraphs, thematic breaks, empty lines) | `MarkdownView.Parsing.cs` | Medium |
| **1t** | Phase 1 unit tests | `Tests/` | Low |
| **2** | AST inline walker (`WalkInlines`) | `MarkdownView.Parsing.cs` | Medium |
| **2t** | Phase 2 unit tests | `Tests/` | Low |
| **3** | Table AST -> `TableData` conversion (Option A: extract cell text) | `MarkdownView.Parsing.cs` | Medium |
| **3t** | Phase 3 unit tests | `Tests/` | Low |
| **4** | Code block AST handling (fenced + indented) | `MarkdownView.Parsing.cs` | Low |
| **4t** | Phase 4 unit tests | `Tests/` | Low |
| **5** | Nested block flattening (quotes, lists, nesting) | `MarkdownView.Parsing.cs` | High |
| **5t** | Phase 5 unit tests | `Tests/` | Medium |
| **6** | Wire up, delete regex code, cleanup, un-skip should-fail tests | `MarkdownView.Parsing.cs`, `Markdown.cs` | Medium |
| **6t** | Phase 6 integration tests, un-skip all Phase 0b tests | `Tests/` | Low |

**Files modified:** `MarkdownView.Parsing.cs`, `Markdown.cs`, test files.
**Files NOT modified:** `MarkdownView.Layout.cs`, `MarkdownView.Drawing.cs`, `MarkdownCodeBlock.cs`, `MarkdownTable.cs`, `IntermediateBlock.cs`, `InlineRun.cs`, `RenderedLine.cs`, `StyledSegment.cs` — the entire downstream pipeline stays the same.
