commit 67732818a3c25804ae6462f9d43015b358744686
Author: miguel <miguel.de.icaza@gmail.com>
Date:   Sun May 13 22:25:16 2018 -0400

    Make the demo work better on WIndows

diff --git a/Designer/Designer.csproj b/Designer/Designer.csproj
index 91afb5c..a81a74d 100644
--- a/Designer/Designer.csproj
+++ b/Designer/Designer.csproj
@@ -1,4 +1,4 @@
-<?xml version="1.0" encoding="utf-8"?>
+﻿<?xml version="1.0" encoding="utf-8"?>
 <Project DefaultTargets="Build" ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
   <PropertyGroup>
     <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
@@ -7,7 +7,8 @@
     <OutputType>Exe</OutputType>
     <RootNamespace>Designer</RootNamespace>
     <AssemblyName>Designer</AssemblyName>
-    <TargetFrameworkVersion>v4.7</TargetFrameworkVersion>
+    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
+    <TargetFrameworkProfile />
   </PropertyGroup>
   <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|x86' ">
     <DebugSymbols>true</DebugSymbols>
@@ -48,6 +49,7 @@
     </ProjectReference>
   </ItemGroup>
   <ItemGroup>
+    <None Include="app.config" />
     <None Include="packages.config" />
   </ItemGroup>
   <Import Project="$(MSBuildBinPath)\Microsoft.CSharp.targets" />
diff --git a/Example/demo.cs b/Example/demo.cs
index 8c4eb89..d6f95af 100644
--- a/Example/demo.cs
+++ b/Example/demo.cs
@@ -153,15 +153,14 @@ static class Demo {
 		ml2 = new Label (1, 1, "Mouse Debug Line");
 		d.Add (ml2);
 		Application.Run (d);
-	}
-
-	// 
-	// Creates a nested editor
-	static void Editor (Toplevel top)
-	{
+	}
+
+	// 
+	// Creates a nested editor
+	static void Editor(Toplevel top) {
 		var tframe = top.Frame;
-		var ntop = new Toplevel (tframe);
-		var menu = new MenuBar (new MenuBarItem [] {
+		var ntop = new Toplevel(tframe);
+		var menu = new MenuBar(new MenuBarItem[] {
 			new MenuBarItem ("_File", new MenuItem [] {
 				new MenuItem ("_Close", "", () => {Application.RequestStop ();}),
 			}),
@@ -171,18 +170,27 @@ static class Demo {
 				new MenuItem ("_Paste", "", null)
 			}),
 		});
-		ntop.Add (menu);
-
-		var win = new Window ("/etc/passwd") {
+		ntop.Add(menu);
+
+		string fname = null;
+		foreach (var s in new[] { "/etc/passwd", "c:\\windows\\win.ini" })
+			if (System.IO.File.Exists(s)) {
+				fname = s;
+				break;
+			}
+
+		var win = new Window(fname ?? "Untitled") {
 			X = 0,
-			Y = 0,
-			Width = Dim.Fill (),
-			Height = Dim.Fill ()
+			Y = 1,
+			Width = Dim.Fill(),
+			Height = Dim.Fill()
 		};
-		ntop.Add (win);
+		ntop.Add(win);
 
-		var text = new TextView (new Rect (0, 0, tframe.Width - 2, tframe.Height - 3));
-		text.Text = System.IO.File.ReadAllText ("/etc/passwd");
+		var text = new TextView(new Rect(0, 0, tframe.Width - 2, tframe.Height - 3));
+		
+		if (fname != null)
+			text.Text = System.IO.File.ReadAllText (fname);
 		win.Add (text);
 
 		Application.Run (ntop);
