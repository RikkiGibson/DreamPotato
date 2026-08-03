namespace LibSampleRateDotNet
{
    public unsafe partial struct SRC_DATA
    {
        [NativeTypeName("const float *")]
        public float* data_in;

        public float* data_out;

        [NativeTypeName("long")]
        public System.Runtime.InteropServices.CLong input_frames;

        [NativeTypeName("long")]
        public System.Runtime.InteropServices.CLong output_frames;

        [NativeTypeName("long")]
        public System.Runtime.InteropServices.CLong input_frames_used;

        [NativeTypeName("long")]
        public System.Runtime.InteropServices.CLong output_frames_gen;

        public int end_of_input;

        public double src_ratio;
    }
}
