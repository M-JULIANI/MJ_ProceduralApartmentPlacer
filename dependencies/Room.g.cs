//----------------------
// <auto-generated>
//     Generated using the NJsonSchema v10.1.21.0 (Newtonsoft.Json v12.0.0.0) (http://NJsonSchema.org)
// </auto-generated>
//----------------------
using Elements;
using Elements.GeoJSON;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Validators;
using Elements.Serialization.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using Line = Elements.Geometry.Line;
using Polygon = Elements.Geometry.Polygon;

namespace Elements
{
    #pragma warning disable // Disable all warnings

    /// <summary>Represents a single room.</summary>
    [Newtonsoft.Json.JsonConverter(typeof(Elements.Serialization.JSON.JsonInheritanceConverter), "discriminator")]
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v12.0.0.0)")]
    public partial class Room : GeometricElement
    {
        [Newtonsoft.Json.JsonConstructor]
        public Room(Polygon @perimeter, Vector3 @direction, string @suiteName, string @suiteNumber, string @department, string @number, double @designArea, double @designRatio, double @rotation, string @levelName, double @elevation, double @height, double @area, Transform @transform, Material @material, Representation @representation, bool @isElementDefinition, System.Guid @id, string @name)
            : base(transform, material, representation, isElementDefinition, id, name)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<Room>();
            if(validator != null)
            {
                validator.PreConstruct(new object[]{ @perimeter, @direction, @suiteName, @suiteNumber, @department, @number, @designArea, @designRatio, @rotation, @levelName, @elevation, @height, @area, @transform, @material, @representation, @isElementDefinition, @id, @name});
            }
        
            this.Perimeter = @perimeter;
            this.Direction = @direction;
            this.SuiteName = @suiteName;
            this.SuiteNumber = @suiteNumber;
            this.Department = @department;
            this.Number = @number;
            this.DesignArea = @designArea;
            this.DesignRatio = @designRatio;
            this.Rotation = @rotation;
            this.LevelName = @levelName;
            this.Elevation = @elevation;
            this.Height = @height;
            this.Area = @area;
            
            if(validator != null)
            {
                validator.PostConstruct(this);
            }
        }
    
        /// <summary>The id of the polygon to extrude.</summary>
        [Newtonsoft.Json.JsonProperty("Perimeter", Required = Newtonsoft.Json.Required.AllowNull)]
        public Polygon Perimeter { get; set; }
    
        /// <summary>The direction in which to extrude.</summary>
        [Newtonsoft.Json.JsonProperty("Direction", Required = Newtonsoft.Json.Required.AllowNull)]
        public Vector3 Direction { get; set; }
    
        /// <summary>Name of the suite assigned to the room.</summary>
        [Newtonsoft.Json.JsonProperty("Suite Name", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string SuiteName { get; set; }
    
        /// <summary>Number of the suite assigned to the room.</summary>
        [Newtonsoft.Json.JsonProperty("Suite Number", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string SuiteNumber { get; set; }
    
        /// <summary>Name of the department assigned to the room.</summary>
        [Newtonsoft.Json.JsonProperty("Department", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string Department { get; set; }
    
        /// <summary>The number of the room.</summary>
        [Newtonsoft.Json.JsonProperty("Number", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string Number { get; set; }
    
        /// <summary>Desired area of the room.</summary>
        [Newtonsoft.Json.JsonProperty("Design Area", Required = Newtonsoft.Json.Required.Always)]
        public double DesignArea { get; set; }
    
        /// <summary>Desired ratio of the X to Y dimensions of the room.</summary>
        [Newtonsoft.Json.JsonProperty("Design Ratio", Required = Newtonsoft.Json.Required.Always)]
        public double DesignRatio { get; set; }
    
        /// <summary>The rotation in degrees of the room.</summary>
        [Newtonsoft.Json.JsonProperty("Rotation", Required = Newtonsoft.Json.Required.Always)]
        public double Rotation { get; set; }
    
        /// <summary>The level name of the room.</summary>
        [Newtonsoft.Json.JsonProperty("Level Name", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string LevelName { get; set; }
    
        /// <summary>The elevation of the room.</summary>
        [Newtonsoft.Json.JsonProperty("Elevation", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Range(0D, double.MaxValue)]
        public double Elevation { get; set; }
    
        /// <summary>The height of the room.</summary>
        [Newtonsoft.Json.JsonProperty("Height", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Range(0D, double.MaxValue)]
        public double Height { get; set; }
    
        /// <summary>The area of the room.</summary>
        [Newtonsoft.Json.JsonProperty("Area", Required = Newtonsoft.Json.Required.Always)]
        public double Area { get; set; }
    
    
    }
}