all: doc-update yaml

# Used to fetch XML doc updates from the C# compiler into the ECMA docs
doc-update:
	mdoc update -i Terminal.Gui/bin/Release/Terminal.Gui.xml -o ecmadocs/en Terminal.Gui/bin/Release/Terminal.Gui.dll

yaml:
	-rm ecmadocs/en/ns-.xml
	mono /cvs/ECMA2Yaml/ECMA2Yaml/ECMA2Yaml/bin/Debug/ECMA2Yaml.exe --source=`pwd`/ecmadocs/en --output=`pwd`/docfx/api
	(cd docfx; mono ~/Downloads/docfx/docfx.exe build)

