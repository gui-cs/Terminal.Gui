all: doc-update yaml

# Used to fetch XML doc updates from the C# compiler into the ECMA docs
doc-update: Terminal.Gui/bin/Release/ne461/Terminal.Gui.dll
	msbuild /p:Configuration=Release
	mdoc update -i Terminal.Gui/bin/Release/net461/Terminal.Gui.xml -o ecmadocs/en Terminal.Gui/bin/Release/net461/Terminal.Gui.dll

Terminal.Gui/bin/Release/ne461/Terminal.Gui.dll: 
	(cd Terminal.Gui)
	msbuild /p:Configuration=Release

yaml:
	-rm ecmadocs/en/ns-.xml
	mono /cvs/ECMA2Yaml/ECMA2Yaml/ECMA2Yaml/bin/Debug/ECMA2Yaml.exe --source=`pwd`/ecmadocs/en --output=`pwd`/docfx/api
	(cd docfx; mono ~/Downloads/docfx/docfx.exe build)

