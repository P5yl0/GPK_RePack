﻿namespace GPK_RePack.Class.Prop
{
    class GpkStringProperty : GpkBaseProperty 
    {
        public long unk;
        public long length;
        public string value;

        public bool IsUnicode = false;

        public GpkStringProperty()
        {
            
        }
        public GpkStringProperty(GpkBaseProperty bp)
        {
            Name = bp.Name;
            type = bp.type;
        }
    }

}
