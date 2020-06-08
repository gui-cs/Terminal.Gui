# Automated Unit Tests

`Terminal.Gui` uses [xunit](https://xunit.net/) for automated unit tests run automatically with Github Actions.

## Notes

* Running tests in parallel is disabled because `Application` is a singleton. Do not change those settings.

## Guidelines for Adding More Tests

1. Do. Please. Add lots.
2. Structure the tests by class. Name the test classes in the form of `ClassNameTests` and the file `ClassNameTests.cs`.
3. The test functions themselves should have descriptive names like `TestBeginEnd`.
4. IMPORTANT: Remember `Application` is a static class (singleton). You must clean up after your tests by calling `Application.Shutdown`.
