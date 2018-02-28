all: doc-update yaml

# Used to fetch XML doc updates from the C# compiler into the ECMA docs
doc-update:
	msbuild /p:Configuration=Release
	mdoc update -i Terminal.Gui/bin/Release/Terminal.Gui.xml -o ecmadocs/en Terminal.Gui/bin/Release/Terminal.Gui.dll

yaml:
	-rm ecmadocs/en/ns-.xml
	mono /cvs/ECMA2Yaml/ECMA2Yaml/ECMA2Yaml/bin/Debug/ECMA2Yaml.exe --source=`pwd`/ecmadocs/en --output=`pwd`/docfx/api
	(cd docfx; mono ~/Downloads/docfx/docfx.exe build)

comp:
	git show 8d6deb10a07c20b10c66dc1cc65ae920ddcf5193:Terminal.Gui/Flex.cs > copy
	diff -u copy /cvs/Xamarin.Forms/Xamarin.Flex/Flex.cs | less
