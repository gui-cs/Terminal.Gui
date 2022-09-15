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
}
