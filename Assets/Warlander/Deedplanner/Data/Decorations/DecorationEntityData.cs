﻿using System.Globalization;
using System.Xml;

namespace Warlander.Deedplanner.Data.Decorations
{
    public class DecorationEntityData : EntityData
    {

        public float X { get; }
        public float Y { get; }

        public DecorationEntityData(int floor, EntityType type, float x, float y) : base(floor, type)
        {
            X = x;
            Y = y;
        }

        public override void Serialize(XmlDocument document, XmlElement localRoot)
        {
            localRoot.SetAttribute("x", X.ToString(CultureInfo.InvariantCulture));
            localRoot.SetAttribute("y", Y.ToString(CultureInfo.InvariantCulture));
        }
        
        public override bool Equals(object other)
        {
            if (!(other is DecorationEntityData data))
            {
                return false;
            }

            return Floor == data.Floor && Type == data.Type && X == data.X && Y == data.Y;
        }

        public override int GetHashCode()
        {
            return (int)Type * 100 + Floor + (int)(X * 1000000) + (int)(Y * 100000000);
        }

        public override string ToString()
        {
            return "Entity floor " + Floor + " type " + Type + " X " + X + " Y " + Y;
        }

    }
}