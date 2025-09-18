# acad-p-id-converter
Simple AutoCAD P&amp;ID converter to create blocks with tags from text elements that match a certain pattern

# This is a PoC only and not intended for production purposes.

Batch converts all Text and MText of drawings in a folder into individual Blocks with configurable attributes which is filled with the same text. The original MText and Text elements are deleted.

This provides a very basic Smart P&ID that can be loaded into a 3rd party program that recognises tags.

Usage:

Compile with Visual Studio and load the dll into AutoCAD with "netload" command.

Edit the config.yaml file with the regex pattern of equipment to match and the name of the tag associated with the pattern.

Execute the command "BatchConvert".

To check correctness of output a debug command "BatchConvertDebug" is provided which leaves the origian MText and Text elements, and offsets the Block text slightly so that both versions of the text can be seen. Elements that are not touched are grayed out.







