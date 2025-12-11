import re
import sys

def add_timestamps_to_events(content):
    """Add Timestamp = currentTime to MouseEventArgs declarations"""
    # Handle inline declarations first
    content = re.sub(
        r'new MouseEventArgs \{ Position =',
        r'new MouseEventArgs { Timestamp = currentTime, Position =',
        content
    )
    content = re.sub(
        r'new MouseEventArgs \{ Flags =',
        r'new MouseEventArgs { Timestamp = currentTime, Flags =',
        content
    )
    
    # Clean up doubles
    content = re.sub(
        r'Timestamp = currentTime, Timestamp = currentTime,',
        r'Timestamp = currentTime,',
        content
    )
    
    return content

def fix_mouse_tests(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Fix constructors
    content = re.sub(
        r'MouseInterpreter interpreter = new \(\(\) => currentTime, ',
        r'MouseInterpreter interpreter = new (',
        content
    )
    content = re.sub(
        r'MouseButtonClickTracker tracker = new \(\(\) => (\w+), ',
        r'MouseButtonClickTracker tracker = new (',
        content
    )
    content = re.sub(
        r'MouseButtonClickTracker tracker(\d+) = new \(\(\) => (\w+), ',
        r'MouseButtonClickTracker tracker\1 = new (',
        content
    )
    
    # Add timestamps
    content = add_timestamps_to_events(content)
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"Fixed {filepath}")

if __name__ == '__main__':
    fix_mouse_tests(r'Tests\UnitTestsParallelizable\Drivers\Mouse\MouseInterpreterExtendedTests.cs')
    fix_mouse_tests(r'Tests\UnitTestsParallelizable\Drivers\Mouse\MouseButtonClickTrackerTests.cs')
    print("\n??  IMPORTANT: Tests still expect OLD immediate-click behavior!")
    print("They need manual updates to expect deferred clicks:")
    print("  - Release events: expect Single() not Equal(2)")  
    print("  - Next action: expect pending click + new event")
    print("  - See MouseButtonClickTrackerTests for pattern")
