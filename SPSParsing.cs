public class SPSParsing
{
    int vBitCount = 0;
    int vBitLength = 0;

    private byte get_bit(byte[] bbase, int offset)
    {
        int vCurBytes = (vBitCount + offset) >> 3;

        offset = (vBitCount + offset) & 0x7;

        if (vCurBytes < bbase.Length)
        {
            return (byte)(((byte)((bbase[vCurBytes])) >> ((byte)0x7 - (byte)(offset & 7))) & (byte)1);
        }
        else
        {
            //'Console.WriteLine("exception in sps parsing, cur bytes is " & vCurBytes & "/" & (base.Length - 1))
            //'Stop
            return 0;
        }
    }

    private byte read_bits(byte[] pBuf, int vReadBits)
    {
        int vCurBytes = vBitCount / 8;
        int vCurBits = vBitCount % 8;
        int vOffset = 0;
        byte vTmp = 0;
        byte vTmp2 = 0;

        if (vReadBits == 1)
        {
            vTmp = get_bit(pBuf, vOffset);
        }
        else
        {
            for (int i = 0; i < vReadBits; i++)
            {
                vTmp2 = get_bit(pBuf, i);
                vTmp = (byte)((byte)(vTmp << 1) + vTmp2);
            }
        }

        vBitCount += vReadBits;

        return vTmp;
    }

    private int ue(byte[] bbase, int offset)
    {
        int zeros = 0;
        int vTmp = 0;
        int vreturn = 0;
        int vIdx = offset;

        for (int _Loop = 1; _Loop <= 1000; _Loop++)
        {
            vTmp = get_bit(bbase, vIdx);
            vIdx++;

            if (vTmp == 0)
                zeros++;
            else
                break;
        }

        if (zeros == 0)
        {
            vBitCount++;
            return 0;
        }

        vreturn = 1 << zeros;

        for (int i = zeros - 1; i >= 0; i--)
        {
            vTmp = get_bit(bbase, vIdx);
            vreturn = vreturn | (vTmp << i);
            vIdx++;
        }

        vBitCount += zeros * 2 + 1;

        return vreturn - 1;
    }

    private int se(byte[] bbase, int offset)
    {
        int zeros = 0;
        int vTmp = 0;
        int vreturn = 0;
        int vIdx = offset;

        for (int _Loop = 1; _Loop <= 1000; _Loop++)
        {
            vTmp = get_bit(bbase, vIdx);
            vIdx++;

            if (vTmp == 0)
                zeros++;
            else
                break;
        }

        if (zeros == 0)
        {
            vBitCount++;
            return 0;
        }

        vreturn = 1 << zeros;

        for (int i = zeros - 1; i >= 0; i--)
        {
            vTmp = get_bit(bbase, vIdx);
            vreturn = vreturn | (vTmp << i);
            vIdx++;
        }

        vBitCount += zeros * 2 + 1;

        if ((vreturn & 1) == 1)
            vreturn = -1 * (vreturn / 2);
        else
            vreturn = (vreturn / 2);


        return vreturn;
    }

    private bool byte_aligned()
    {
        if ((vBitCount & 7) == 0)
            return true;
        else
            return false;
    }

    private HrdParameters hrd_parameters(byte[] pSPSBytes)
    {
        int cpb_cnt_minus1 = ue(pSPSBytes, 0);
        HrdParameters r = new HrdParameters();

        r.cpb_cnt_minus1 = cpb_cnt_minus1;
        r.bit_rate_scale = read_bits(pSPSBytes, 4);
        r.cpb_size_scale = read_bits(pSPSBytes, 4);

        for (int SchedSelIdx = 0; SchedSelIdx <= cpb_cnt_minus1; SchedSelIdx++)
        {
            HrdParameters.cpbMinus1Content cpb_minus1 = new HrdParameters.cpbMinus1Content();

            cpb_minus1.bit_rate_value_minus1 = ue(pSPSBytes, 0);
            cpb_minus1.cpb_size_value_minus1 = ue(pSPSBytes, 0);
            cpb_minus1.cbr_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));

            r.cpb_minus1.Add(cpb_minus1);
        }

        r.initial_cpb_removal_delay_length_minus1 = read_bits(pSPSBytes, 5);
        r.cpb_removal_delay_length_minus1 = read_bits(pSPSBytes, 5);
        r.dpb_output_delay_length_minus1 = read_bits(pSPSBytes, 5);
        r.time_offset_length = read_bits(pSPSBytes, 5);

        return r;
    }

    private VuiParameters vui_parameters(byte[] pSPSBytes)
    {
        VuiParameters r = new VuiParameters();

        r.aspect_ratio_info_present_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
        if (r.aspect_ratio_info_present_flag)
        {
            r.aspect_ratio_idc = read_bits(pSPSBytes, 8);
            if (r.aspect_ratio_idc == 1)
            {
                r.sar_width = read_bits(pSPSBytes, 16);
                r.sar_height = read_bits(pSPSBytes, 16);
            }
        }

        r.overscan_info_present_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
        if (r.overscan_info_present_flag)
            r.overscan_appropriate_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));

        r.video_signal_type_present_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
        if (r.video_signal_type_present_flag)
        {
            r.video_format = read_bits(pSPSBytes, 3);
            r.video_full_range_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
            r.colour_description_present_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
            if (r.colour_description_present_flag)
            {
                r.colour_primaries = read_bits(pSPSBytes, 8);
                r.transfer_characteristics = read_bits(pSPSBytes, 8);
                r.matrix_coefficients = read_bits(pSPSBytes, 8);
            }
        }

        r.chroma_loc_info_present_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
        if (r.chroma_loc_info_present_flag)
        {
            r.chroma_sample_loc_type_top_field = ue(pSPSBytes, 0);
            r.chroms_sample_loc_type_bottom_field = ue(pSPSBytes, 0);
        }

        r.timing_info_present_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
        if (r.timing_info_present_flag)
        {
            r.num_units_in_tick = read_bits(pSPSBytes, 32);
            r.time_scale = read_bits(pSPSBytes, 32);
            r.fixed_frame_rate_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
        }

        r.nal_hrd_parameters_present_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
        if (r.nal_hrd_parameters_present_flag)
        {
            r.nal_hrd_parameters = hrd_parameters(pSPSBytes);
        }

        r.vcl_hrd_parameters_present_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
        if (r.vcl_hrd_parameters_present_flag)
        {
            r.vcl_hrd_parameters = hrd_parameters(pSPSBytes);
        }

        if (r.nal_hrd_parameters_present_flag || r.vcl_hrd_parameters_present_flag)
        {
            r.low_delay_hrd_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
        }

        r.pic_struct_present_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
        r.bitstream_restriction_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
        if (r.bitstream_restriction_flag)
        {
            r.motion_vectors_over_pic_boundaries_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
            r.max_bytes_per_pic_denom = ue(pSPSBytes, 0);
            r.max_bits_per_mb_denom = ue(pSPSBytes, 0);
            r.log2_max_mv_length_horizontal = ue(pSPSBytes, 0);
            r.log2_max_mv_length_vertical = ue(pSPSBytes, 0);
            r.num_reorder_frames = ue(pSPSBytes, 0);
            r.max_dec_frame_buffering = ue(pSPSBytes, 0);
        }

        return r;
    }

    private bool rbsp_trailing_bits(byte[] pInput)
    {
        bool rbsp_stop_one_bit = false;
        bool rbsp_alignment_zero_bit;

        if (vBitCount + 1 >= vBitLength)
            return true;

        rbsp_stop_one_bit = System.Convert.ToBoolean(read_bits(pInput, 1));
        while (!byte_aligned())
        {
            rbsp_alignment_zero_bit = System.Convert.ToBoolean(read_bits(pInput, 1));
        }

        return true;
    }

    public SeqParameterSet seq_parameter_set_rbsp(byte[] buffer)
    {
        SeqParameterSet r = new SeqParameterSet();
        byte[] pSPSBytes = Filter003(buffer);

        vBitCount = 0;
        vBitLength = pSPSBytes.Length * 8;

        r.forbidden_zero_bit = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
        r.nal_ref_idc = read_bits(pSPSBytes, 2);
        r.nal_unit_type = read_bits(pSPSBytes, 5);

        r.profile_idc = read_bits(pSPSBytes, 8);
        r.profile_compat = read_bits(pSPSBytes, 5);
        //'r.constrained_set0_flag = read_bits(pSPSBytes, 1)
        //'r.constrained_set1_flag = read_bits(pSPSBytes, 1)
        //'r.constrained_set2_flag = read_bits(pSPSBytes, 1)
        //'r.constrained_set3_flag = read_bits(pSPSBytes, 1)
        //'r.constrained_set4_flag = read_bits(pSPSBytes, 1)
        r.reserved_zero_3bits = read_bits(pSPSBytes, 3);

        r.level_idc = read_bits(pSPSBytes, 8);

        if ((r.profile_idc == 100) || (r.profile_idc == 110) || (r.profile_idc == 122) ||
       (r.profile_idc == 244) || (r.profile_idc == 44) || (r.profile_idc == 83) ||
       (r.profile_idc == 86) || (r.profile_idc == 118))
            r.seq_parameter_set_id = 0;
        else
            r.seq_parameter_set_id = ue(pSPSBytes, 0);


        r.log2_max_frame_num_minute4 = ue(pSPSBytes, 0);

        r.pic_order_cnt_type = ue(pSPSBytes, 0);

        if (r.pic_order_cnt_type == 0)
        {
            r.log2_max_pic_order_cnt_lsb_minus4 = ue(pSPSBytes, 0);
        }
        else if (r.pic_order_cnt_type == 1)
        {
            int num_ref_frames_in_pic_order_cnt_cycle = 0;

            r.delta_pic_order_always_zero_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
            r.offset_for_non_ref_pic = se(pSPSBytes, 0);
            r.offset_for_top_to_bottom_field = se(pSPSBytes, 0);

            num_ref_frames_in_pic_order_cnt_cycle = ue(pSPSBytes, 0);

            for (int i = 0; i < num_ref_frames_in_pic_order_cnt_cycle; i++)
                r.num_ref_frames_in_pic_order_cnt_cycle.Add(se(pSPSBytes, 0));
        }


        r.num_ref_frames = ue(pSPSBytes, 0);
        r.gaps_in_frame_num_value_allowed_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));

        r.pic_width_in_mbs_minus1 = ue(pSPSBytes, 0);
        r.pic_height_in_map_units_minus1 = ue(pSPSBytes, 0);

        r.frame_mbs_only_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));

        if (r.frame_mbs_only_flag == false)
            r.mb_adaptive_frame_field_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));


        r.direct_8x8_interence_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
        r.frame_cropping_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));
        if (r.frame_cropping_flag)
        {
            r.frame_cropping_rect_left_offset = ue(pSPSBytes, 0);
            r.frame_cropping_rect_right_offset = ue(pSPSBytes, 0);
            r.frame_cropping_rect_top_offset = ue(pSPSBytes, 0);
            r.frame_cropping_rect_bottom_offset = ue(pSPSBytes, 0);
        }

        r.vui_parameters_present_flag = System.Convert.ToBoolean(read_bits(pSPSBytes, 1));

        if (r.vui_parameters_present_flag)
        {
            try { r.vui_parameters = vui_parameters(pSPSBytes); }
            catch (System.Exception ex) { }
        }

        rbsp_trailing_bits(pSPSBytes);

        return r;
    }

    public PicParameterSet pic_parameter_set_rbsp(byte[] buffer)
    {
        PicParameterSet r = new PicParameterSet();
        byte[] pPPSBytes = Filter003(buffer);


        vBitCount = 0;
        vBitLength = pPPSBytes.Length * 8;

        r.forbidden_zero_bit = System.Convert.ToBoolean(read_bits(pPPSBytes, 1));
        r.nal_ref_idc = read_bits(pPPSBytes, 2);
        r.nal_unit_type = read_bits(pPPSBytes, 5);

        r.pic_parameter_set_id = ue(pPPSBytes, 0);
        r.seq_parameter_set_id = ue(pPPSBytes, 0);
        r.entropy_coding_mode_flag = System.Convert.ToBoolean(read_bits(pPPSBytes, 1));
        r.pic_order_present_flag = System.Convert.ToBoolean(read_bits(pPPSBytes, 1));
        r.num_slice_groups_minus1 = ue(pPPSBytes, 0);

        if (r.num_slice_groups_minus1 > 0)
        {
            r.slice_group_map_type = ue(pPPSBytes, 0);
            if (r.slice_group_map_type == 0)
            {
                for (int iGroup = 0; iGroup <= r.num_slice_groups_minus1; iGroup++)
                {
                    r.run_length_minus1.Add(ue(pPPSBytes, 0));
                }
            }
            else if (r.slice_group_map_type == 2)
            {
                for (int iGroup = 0; iGroup <= r.num_slice_groups_minus1; iGroup++)
                {
                    PicParameterSet.SliceGroup Group = new PicParameterSet.SliceGroup();

                    Group.top_left = ue(pPPSBytes, 0);
                    Group.bottom_right = ue(pPPSBytes, 0);

                    r.slice_groups.Add(Group);
                }
            }
            else if ((r.slice_group_map_type == 3) || (r.slice_group_map_type == 4) || (r.slice_group_map_type == 5))
            {
                r.slice_group_change_direction_flag = System.Convert.ToBoolean(read_bits(pPPSBytes, 1));
                r.slice_group_change_rate_minus1 = ue(pPPSBytes, 0);
            }
            else if (r.slice_group_map_type == 6)
            {
                r.pic_size_in_map_units_minus1 = ue(pPPSBytes, 0);

                for (int iGroup = 0; iGroup <= r.pic_size_in_map_units_minus1; iGroup++)
                {
                    r.slice_group_id.Add(ue(pPPSBytes, 0));
                }
            }
        }

        r.num_ref_idx_l0_active_minus1 = ue(pPPSBytes, 0);
        r.num_ref_idx_l1_active_minus1 = ue(pPPSBytes, 0);
        r.weighted_pref_flag = System.Convert.ToBoolean(read_bits(pPPSBytes, 1));
        r.weighted_bipred_idc = read_bits(pPPSBytes, 2);

        r.pic_init_qp_minus26 = se(pPPSBytes, 0);
        r.pic_init_qs_minus26 = se(pPPSBytes, 0);

        r.chroma_qp_index_offset = se(pPPSBytes, 0);

        r.deblocking_filter_control_present_flag = System.Convert.ToBoolean(read_bits(pPPSBytes, 1));
        r.constrained_intra_pred_flag = System.Convert.ToBoolean(read_bits(pPPSBytes, 1));
        r.redundant_pic_cnt_present_flag = System.Convert.ToBoolean(read_bits(pPPSBytes, 1));

        rbsp_trailing_bits(pPPSBytes);

        return r;
    }

    private byte[] Filter003(byte[] buffer)
    {
        System.Collections.Generic.List<byte> tmpArray = new System.Collections.Generic.List<byte>();
        int I = 0;

        while (true)
        {
            if (I >= buffer.Length)
                break;

            if (I < (buffer.Length - 3))
            {
                if ((buffer[I] == 0) &&
                   (buffer[I + 1] == 0) &&
                   (buffer[I + 2] == 3))
                {
                    tmpArray.AddRange(new byte[] { 0, 0 });
                    I += 3;
                }
                else
                {
                    tmpArray.Add(buffer[I]);
                    I += 1;
                }
            }
            else
            {
                tmpArray.Add(buffer[I]);
                I++;
            }
        }

        return tmpArray.ToArray();
    }

    public class HrdParameters
    {
        public int cpb_cnt_minus1;
        public byte bit_rate_scale;
        public byte cpb_size_scale;
        public System.Collections.Generic.List<cpbMinus1Content> cpb_minus1 = new System.Collections.Generic.List<cpbMinus1Content>();
        public byte initial_cpb_removal_delay_length_minus1;
        public byte cpb_removal_delay_length_minus1;
        public byte dpb_output_delay_length_minus1;
        public byte time_offset_length;

        public class cpbMinus1Content
        {
            public int bit_rate_value_minus1;
            public int cpb_size_value_minus1;
            public bool cbr_flag;
        }
    }

    public class VuiParameters
    {
        public bool aspect_ratio_info_present_flag;
        public byte aspect_ratio_idc;
        public ushort sar_width;
        public ushort sar_height;

        public bool overscan_info_present_flag;
        public bool overscan_appropriate_flag;

        public bool video_signal_type_present_flag;
        public int video_format;
        public bool video_full_range_flag;
        public bool colour_description_present_flag;
        public byte colour_primaries;
        public byte transfer_characteristics;
        public byte matrix_coefficients;

        public bool chroma_loc_info_present_flag;
        public int chroma_sample_loc_type_top_field;
        public int chroms_sample_loc_type_bottom_field;

        public bool timing_info_present_flag;
        public uint num_units_in_tick;
        public uint time_scale;
        public bool fixed_frame_rate_flag;

        public bool nal_hrd_parameters_present_flag;
        public HrdParameters nal_hrd_parameters;

        public bool vcl_hrd_parameters_present_flag;
        public HrdParameters vcl_hrd_parameters;

        public bool low_delay_hrd_flag;

        public bool pic_struct_present_flag;
        public bool bitstream_restriction_flag;
        public bool motion_vectors_over_pic_boundaries_flag;

        public int max_bytes_per_pic_denom;
        public int max_bits_per_mb_denom;
        public int log2_max_mv_length_horizontal;
        public int log2_max_mv_length_vertical;
        public int num_reorder_frames;
        public int max_dec_frame_buffering;
    }

    public class SeqParameterSet
    {
        public bool forbidden_zero_bit;
        public byte nal_ref_idc;
        public byte nal_unit_type;

        public byte profile_idc;
        public byte profile_compat;

        //'public constrained_set0_flag As Boolean
        //'public constrained_set1_flag As Boolean
        //'public constrained_set2_flag As Boolean
        //'public constrained_set3_flag As Boolean
        //'public constrained_set4_flag As Boolean
        public byte reserved_zero_3bits;

        public byte level_idc;

        public int seq_parameter_set_id;

        public int log2_max_frame_num_minute4;
        public int pic_order_cnt_type;
        public int log2_max_pic_order_cnt_lsb_minus4;


        public bool delta_pic_order_always_zero_flag;
        public int offset_for_non_ref_pic;
        public int offset_for_top_to_bottom_field;

        public System.Collections.Generic.List<int> num_ref_frames_in_pic_order_cnt_cycle = new System.Collections.Generic.List<int>();


        public int num_ref_frames;
        public bool gaps_in_frame_num_value_allowed_flag;

        public int pic_width_in_mbs_minus1;
        public int pic_height_in_map_units_minus1;

        public bool frame_mbs_only_flag;

        public bool mb_adaptive_frame_field_flag;

        public bool direct_8x8_interence_flag;
        public bool frame_cropping_flag;
        public int frame_cropping_rect_left_offset;
        public int frame_cropping_rect_right_offset;
        public int frame_cropping_rect_top_offset;
        public int frame_cropping_rect_bottom_offset;

        public bool vui_parameters_present_flag;

        public VuiParameters vui_parameters;

        public string GetProfileString()
        {
            string RetValue;

            switch (profile_idc)
            {
                case 66:
                    RetValue = "Baseline";
                    break;
                case 77:
                    RetValue = "Main";
                    break;
                case 88:
                    RetValue = "Extended";
                    break;
                case 100:
                    RetValue = "High";
                    break;
                case 110:
                    RetValue = "High10";
                    break;
                case 122:
                    RetValue = "High422";
                    break;
                case 244:
                    RetValue = "High444";
                    break;
                default:
                    RetValue = "Unknow";
                    break;
            }

            return RetValue;
        }

        public string GetProfileLevelId()
        {
            return profile_idc.ToString("X2") + profile_compat.ToString("X2") + level_idc.ToString("X2");
        }

        public int Width()
        {
            return (((pic_width_in_mbs_minus1 + 1) * 16) - frame_cropping_rect_left_offset * 2 - frame_cropping_rect_right_offset * 2);
        }

        public int Height()
        {
            return ((2 - (frame_mbs_only_flag ? 1 : 0)) * (pic_height_in_map_units_minus1 + 1) * 16) - ((frame_mbs_only_flag ? 2 : 4) * (frame_cropping_rect_top_offset + frame_cropping_rect_bottom_offset));
        }
    }

    public class PicParameterSet
    {
        public bool forbidden_zero_bit;
        public byte nal_ref_idc;
        public byte nal_unit_type;

        public int pic_parameter_set_id;
        public int seq_parameter_set_id;
        public bool entropy_coding_mode_flag;
        public bool pic_order_present_flag;
        public int num_slice_groups_minus1;

        public int slice_group_map_type;

        public System.Collections.Generic.List<int> run_length_minus1 = new System.Collections.Generic.List<int>();

        public System.Collections.Generic.List<SliceGroup> slice_groups = new System.Collections.Generic.List<SliceGroup>();

        public bool slice_group_change_direction_flag;
        public int slice_group_change_rate_minus1;

        public int pic_size_in_map_units_minus1;
        public System.Collections.Generic.List<int> slice_group_id = new System.Collections.Generic.List<int>();

        public int num_ref_idx_l0_active_minus1;
        public int num_ref_idx_l1_active_minus1;
        public bool weighted_pref_flag;
        public byte weighted_bipred_idc;

        public int pic_init_qp_minus26;
        public int pic_init_qs_minus26;

        public int chroma_qp_index_offset;

        public bool deblocking_filter_control_present_flag;
        public bool constrained_intra_pred_flag;
        public bool redundant_pic_cnt_present_flag;

        public class SliceGroup
        {
            public int top_left;
            public int bottom_right;
        }
    }
}
