using System;
using System.Reflection;

public static class ReflectionTools {
	// If the class is non-static
	public static Object InvokePrivate (Object objectUnderTest, string method, params object [] args)
	{
		Type t = objectUnderTest.GetType ();
		return t.InvokeMember (method,
		    BindingFlags.InvokeMethod |
		    BindingFlags.NonPublic |
		    BindingFlags.Instance |
		    BindingFlags.Static,
		    null,
		    objectUnderTest,
		    args);
	}
	// if the class is static
	public static Object InvokePrivate (Type typeOfObjectUnderTest, string method, params object [] args)
	{
		MemberInfo [] members = typeOfObjectUnderTest.GetMembers (BindingFlags.NonPublic | BindingFlags.Static);
		foreach (var member in members) {
			if (member.Name == method) {
				return typeOfObjectUnderTest.InvokeMember (method,
					BindingFlags.NonPublic |
					BindingFlags.Static |
					BindingFlags.InvokeMethod,
					null,
					typeOfObjectUnderTest,
					args);
			}
		}
		return null;
	}

	public static T GetFieldValue<T> (this object obj, string name)
	{
		// Set the flags so that private and public fields from instances will be found
		var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		var field = obj.GetType ().GetField (name, bindingFlags);
		return (T)field?.GetValue (obj);
	}
}
