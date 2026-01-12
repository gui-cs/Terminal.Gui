import re

def fix_interpreter_tests(filepath):
    """Fix MouseInterpreterExtendedTests to match deferred click behavior"""
    
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Fix Process_ClickAtDifferentPosition_ResetsClickCount
    # OLD: expects 2 events (release + click) on first release
    # NEW: expects 1 event (release only, click pending)
    content = re.sub(
        r"(Process_ClickAtDifferentPosition_ResetsClickCount.*?)"
        r"Assert\.Equal \(2, events2\.Count\);(.*?)"
        r"Assert\.Contains \(events2, e => e\.Flags == MouseFlags\.Button1Clicked\);",
        r"\1Assert.Single (events2); // Only release event, click is pending\2"
        r"// Click will be yielded on next action (press2)",
        content,
        flags=re.DOTALL
    )
    
    # Update to expect click on press2 (when position changes)
    content = re.sub(
        r"(Process_ClickAtDifferentPosition_ResetsClickCount.*?)"
        r"(currentTime = currentTime\.AddMilliseconds \(50\);.*?)"
        r"(_ = interpreter\.Process \(press2\)\.ToList.*?)"
        r"(currentTime = currentTime\.AddMilliseconds.*?)"
        r"(List<MouseEventArgs> events4 = interpreter\.Process \(release2\)\.ToList.*?)"
        r"(// Assert.*?)"
        r"Assert\.Equal \(2, events4\.Count\);",
        r"\1\2List<MouseEventArgs> events3 = interpreter.Process (press2).ToList (); // Press at different position\3\4\5\6"
        r"// events3 should contain: pending click (from release1) + press2\n        "
        r"Assert.Equal (2, events3.Count); // Pending click + press event\n        "
        r"Assert.Contains (events3, e => e.Flags == MouseFlags.Button1Clicked); // Pending click yielded\n        "
        r"Assert.Contains (events3, e => e.Flags == MouseFlags.Button1Pressed);\n\n        "
        r"Assert.Single (events4); // Only release, new click pending",
        content,
        flags=re.DOTALL,
        count=1
    )
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"Fixed {filepath}")

if __name__ == '__main__':
    fix_interpreter_tests(r'Tests\UnitTestsParallelizable\Drivers\Mouse\MouseInterpreterExtendedTests.cs')
