using System;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace ReactiveExample;
public static class ViewExtensions
{
    public static (Window MainView, TOut LastControl) AddControl<TOut> (this Window view, Action<TOut> action)
        where TOut : View, new()
    {
        TOut result = new ();
        action (result);
        view.Add (result);
        return (view, result);
    }

    public static (Window MainView, TOut LastControl) AddControlAfter<TOut> (this (Window MainView, View LastControl) view, Action<View, TOut> action)
        where TOut : View, new()
    {
        TOut result = new ();
        action (view.LastControl, result);
        view.MainView.Add (result);
        return (view.MainView, result);
    }
}
