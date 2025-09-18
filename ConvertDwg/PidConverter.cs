using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BP.ConvertDwg;

public class PidConverter
{
    private const string BLOCK_NAME = "BentleyBlock";
    private const double TEXT_HEIGHT = 2.25;
    private readonly IDictionary<string, ObjectId> attDefinitions = new Dictionary<string, ObjectId>();
    // HV-2340-103
    // %%UV-2340-01
    private const string EQUIPMENT_PATTERN = @"^[%A-Z0-9]+-[A-Z0-9]+-[A-Z0-9]+$";
    // 4"-CD-4340-010-B01E
    private const string LINE_PATTERN = @"^\d+""-[A-Z0-9]+-[A-Z0-9]+-[A-Z0-9]+-[A-Z0-9]+$";

    public void Convert(Document doc, bool debug, IList<Config.Tag> tags)
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddDebug());
        var logger = factory.CreateLogger(nameof(PidConverter));
        var db = doc.Database;
        var textCorrection = new Vector3d(0, -0.5 * TEXT_HEIGHT, 0);
        var patterns = tags.Select(c => new { c.Name, Regex = new Regex(c.Pattern) }).ToList();
        using var debugColor = Color.FromRgb(75, 75, 75);

        using var tr = db.TransactionManager.StartTransaction();
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
        foreach (var id in modelSpace)
        {
            using var obj = tr.GetObject(id, OpenMode.ForWrite);
            var converted = false;

            if (obj is DBText || obj is MText)
            {
                var text = (obj as DBText)?.TextString ?? ((MText)obj).Contents;
                // if (text.Length > 30) text = text.Substring(0, 30);
                var position = (obj as DBText)?.Position ?? ((MText)obj).Location.Add(textCorrection);
                var alignmentPoint = (obj as DBText)?.AlignmentPoint ?? ((MText)obj).Location.Add(textCorrection);
                var height = (obj as DBText)?.Height ?? ((MText)obj).TextHeight;
                var widthFactor = (obj as DBText)?.WidthFactor ?? 0.9;
                var justify = (obj as DBText)?.Justify ?? AttachmentPoint.MiddleLeft;
                var color = (obj as DBText)?.Color ?? ((MText)obj).Color;
                var rotation = (obj as DBText)?.Rotation ?? ((MText)obj).Rotation;
                var layer = (obj as DBText)?.Layer ?? ((MText)obj).Layer;
                var styleId = (obj as DBText)?.TextStyleId ?? ((MText)obj).TextStyleId;
                logger.LogInformation($"type: {obj.GetType().Name} height: {height} text: {text} widthfactor: {widthFactor} justify: {justify} color: {color} rotation: {rotation} layer: {layer}");

                var match = patterns.FirstOrDefault(c => c.Regex.IsMatch(text));
                if (match != null)
                {
                    var (blkRecId, attRefId) = GetOrCreateBlock(tr, db, match.Name);
                    using var blockRef = new BlockReference(alignmentPoint, blkRecId);
                    var curSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                    curSpaceBlkTblRec.AppendEntity(blockRef);
                    tr.AddNewlyCreatedDBObject(blockRef, true);
                    using var attRef = new AttributeReference();
                    var attDef = tr.GetObject(attRefId, OpenMode.ForRead) as AttributeDefinition;
                    attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
                    attRef.Position = attDef.Position.TransformBy(blockRef.BlockTransform);
                    // attRef.AlignmentPoint = attDef.AlignmentPoint.TransformBy(blockRef.BlockTransform) + (alignmentPoint - position);
                    attRef.TextString = text;
                    attRef.Color = color;
                    attRef.Justify = justify;
                    attRef.WidthFactor = widthFactor;
                    attRef.Rotation = rotation;
                    attRef.Height = (debug ? 1.01 : 1.0) * height;
                    attRef.TextStyleId = styleId;
                    attRef.Visible = true;
                    attRef.Invisible = false;
                    blockRef.AttributeCollection.AppendAttribute(attRef);
                    tr.AddNewlyCreatedDBObject(attRef, true);
                    if (!debug)
                    {
                        obj.Erase();
                    }
                    converted = true;
                }

            }
            if (!converted && debug && (obj is Entity))
            {
                (obj as Entity).Color = debugColor;
            }
        }

        tr.Commit();
    }

    public (ObjectId, ObjectId) GetOrCreateBlock(Transaction tr, Database db, string type)
    {
        // Open the Block table for read
        var blkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
        var blockname = $"{BLOCK_NAME}_{type}";
        if (!blkTbl.Has(blockname))
        {
            using BlockTableRecord blkTblRec = new BlockTableRecord();
            blkTblRec.Name = blockname;

            // Set the insertion point for the block
            blkTblRec.Origin = new Point3d(0, 0, 0);

            // Add an attribute definition to the block
            using AttributeDefinition attDef = new AttributeDefinition();
            attDef.Position = new Point3d(0, 0, 0);
            // attDef.AlignmentPoint = new Point3d(0, 0, 0);
            attDef.Verifiable = true;
            attDef.Prompt = $"ALIM {type}";
            attDef.Tag = type;
            attDef.TextString = string.Empty;
            attDef.Height = TEXT_HEIGHT;
            attDef.Justify = AttachmentPoint.MiddleCenter;
            //show property in palette
            attDef.Visible = true;
            //dont show tag label in drawing area
            attDef.Invisible = false;

            blkTblRec.AppendEntity(attDef);

            tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
            blkTbl.Add(blkTblRec);
            tr.AddNewlyCreatedDBObject(blkTblRec, true);
            attDefinitions.Add(blockname, attDef.Id);
            return (blkTblRec.Id, attDef.Id);
        }
        else
        {
            return (blkTbl[blockname], attDefinitions[blockname]);
        }
    }
}
