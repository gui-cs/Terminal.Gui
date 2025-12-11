import re
import sys

def fix_mouse_tests(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Fix MouseInterpreter constructor - remove time function
    content = re.sub(
        r'MouseInterpreter interpreter = new \(\(\) => currentTime, ',
        r'MouseInterpreter interpreter = new (',
        content
    )
    
    # Fix MouseButtonClickTracker constructor - remove time function  
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
    
    # Add Timestamp to MouseEventArgs that don't have it yet
    # Pattern: new MouseEventArgs { Position = ..., Flags = ...
    # Replace with: new MouseEventArgs { Timestamp = currentTime, Position = ..., Flags = ...
    
    # First handle cases with Position at the beginning
    content = re.sub(
        r'new MouseEventArgs \{ Position = ',
        r'new MouseEventArgs { Timestamp = currentTime, Position = ',
        content
    )
    
    # Handle cases with Flags at the beginning
    content = re.sub(
        r'new MouseEventArgs \{ Flags = ',
        r'new MouseEventArgs { Timestamp = currentTime, Flags = ',
        content
    )
    
    # Clean up double Timestamp assignments (in case we already had some)
    content = re.sub(
        r'Timestamp = currentTime, Timestamp = currentTime, ',
        r'Timestamp = currentTime, ',
        content
    )
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"Fixed {filepath}")

if __name__ == '__main__':
    fix_mouse_tests(r'Tests\UnitTestsParallelizable\Drivers\Mouse\MouseInterpreterExtendedTests.cs')
    fix_mouse_tests(r'Tests\UnitTestsParallelizable\Drivers\Mouse\MouseButtonClickTrackerTests.cs')
