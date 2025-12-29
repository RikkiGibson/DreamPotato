namespace LibSampleRateDotNet
{
    public unsafe partial struct SRC_DATA
    {
        [NativeTypeName("const float *")]
        public float* data_in;

        public float* data_out;

        [NativeTypeName("long")]
        public int input_frames;

        [NativeTypeName("long")]
        public int output_frames;

        [NativeTypeName("long")]
        public int input_frames_used;

        [NativeTypeName("long")]
        public int output_frames_gen;

        public int end_of_input;

        public double src_ratio;
    }
}
