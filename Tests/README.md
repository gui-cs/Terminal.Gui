# Terminal.Gui Tests

This folder contains the tests for Terminal.Gui.

## ./UnitTests

This folder contains the unit tests for Terminal.Gui that can not be run in parallel. This is because they
depend on `Application` or other class that use static state or `ConfigurationManager`.

We should be striving to move as many tests as possible to the `./UnitTestsParallelizable` folder.

## ./UnitTestsParallelizable

This folder contains the unit tests for Terminal.Gui that can be run in parallel.

## ./IntegrationTests

This folder contains the integration tests for Terminal.Gui.

## ./StressTests

This folder contains the stress tests for Terminal.Gui.

## ./PerformanceTests

This folder WILL contain the performance tests for Terminal.Gui.


See the [Testing wiki](https://github.com/gui-cs/Terminal.Gui/wiki/Testing) for details on how to add more tests.
