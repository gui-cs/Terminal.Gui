using Terminal.Gui;

namespace ReactiveExample {
	public static class Extensions
	{
		public static MemoizedElement<TOwner, TNew> StackPanel<TOwner, TNew>(
			this TOwner owner,
			TNew control)
			where TOwner : View
			where TNew : View =>
			new MemoizedElement<TOwner, TNew>(owner, control);

		public static MemoizedElement<TOwner, TNew> Append<TOwner, TOld, TNew>(
			this MemoizedElement<TOwner, TOld> owner, 
			TNew control,
			int height = 1)
			where TOwner : View 
			where TOld : View
			where TNew : View
		{
			control.X = Pos.Left(owner.Control);
			control.Y = Pos.Top(owner.Control) + height;
			return new MemoizedElement<TOwner, TNew>(owner.View, control);
		}

		public class MemoizedElement<TOwner, TControl> 
			where TOwner : View 
			where TControl : View
		{
			public TOwner View { get; }
			public TControl Control { get; }

			public MemoizedElement(TOwner owner, TControl control)
			{
				View = owner;
				Control = control;
				View.Add(control);
			}
		}
	}
}