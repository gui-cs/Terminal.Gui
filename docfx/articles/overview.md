Overview
========

`Terminal.Gui` is a library intended to create console-based
applications using C#.  The framework has been designed to make it
easy to write applications that will work on monochrome terminals, as
well as modern color terminals with mouse support.

This library provides a text-based toolkit as works in a way similar
to graphic toolkits.   There are many controls that can be used to
create your applications and it is event based, meaning that you
create the user interface, hook up various events and then let the
a processing loop run your application, and your code is invoked via
one or more callbacks.

The simplest application looks like this:

```
using Terminal.Gui;

class Demo {
    static int Main ()
    {
        Application.Init ();

	var n = MessageBox.Query (50, 7, "Question", "Do you like console apps?", "Yes", "No");

	return n;
    }
}
```

This example shows a prompt and returns an integer value depending on
which value was selected by the user (Yes, No, or if they use chose
not to make a decision and instead pressed the ESC key).

More interesting user interfaces can be created by composing some of
the various views that are included.   In the following sections, you
will see how applications are put together.

View
====

