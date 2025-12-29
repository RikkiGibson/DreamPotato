using System.Runtime.InteropServices;

namespace LibSampleRateDotNet
{
    public static unsafe partial class LibSampleRate
    {
        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SRC_STATE *")]
        public static extern SRC_STATE_tag* src_new(int converter_type, int channels, int* error);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SRC_STATE *")]
        public static extern SRC_STATE_tag* src_clone([NativeTypeName("SRC_STATE *")] SRC_STATE_tag* orig, int* error);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SRC_STATE *")]
        public static extern SRC_STATE_tag* src_callback_new([NativeTypeName("src_callback_t")] delegate* unmanaged[Cdecl]<void*, float**, int> func, int converter_type, int channels, int* error, void* cb_data);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SRC_STATE *")]
        public static extern SRC_STATE_tag* src_delete([NativeTypeName("SRC_STATE *")] SRC_STATE_tag* state);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int src_process([NativeTypeName("SRC_STATE *")] SRC_STATE_tag* state, SRC_DATA* data);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("long")]
        public static extern int src_callback_read([NativeTypeName("SRC_STATE *")] SRC_STATE_tag* state, double src_ratio, [NativeTypeName("long")] int frames, float* data);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int src_simple(SRC_DATA* data, int converter_type, int channels);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* src_get_name(int converter_type);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* src_get_description(int converter_type);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* src_get_version();

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int src_set_ratio([NativeTypeName("SRC_STATE *")] SRC_STATE_tag* state, double new_ratio);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int src_get_channels([NativeTypeName("SRC_STATE *")] SRC_STATE_tag* state);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int src_reset([NativeTypeName("SRC_STATE *")] SRC_STATE_tag* state);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int src_is_valid_ratio(double ratio);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int src_error([NativeTypeName("SRC_STATE *")] SRC_STATE_tag* state);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* src_strerror(int error);

        public const int SRC_SINC_BEST_QUALITY = 0;
        public const int SRC_SINC_MEDIUM_QUALITY = 1;
        public const int SRC_SINC_FASTEST = 2;
        public const int SRC_ZERO_ORDER_HOLD = 3;
        public const int SRC_LINEAR = 4;

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void src_short_to_float_array([NativeTypeName("const short *")] short* @in, float* @out, int len);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void src_float_to_short_array([NativeTypeName("const float *")] float* @in, short* @out, int len);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void src_int_to_float_array([NativeTypeName("const int *")] int* @in, float* @out, int len);

        [DllImport("samplerate.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void src_float_to_int_array([NativeTypeName("const float *")] float* @in, int* @out, int len);
    }
}
