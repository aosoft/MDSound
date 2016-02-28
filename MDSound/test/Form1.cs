﻿using System;
using System.IO;
using System.Windows.Forms;
using SdlDotNet.Audio;
using System.Threading;

namespace test
{
    public partial class Form1 : Form
    {
        SdlDotNet.Audio.AudioStream sdl;
        static int SamplingRate = 44100;
        static int PSGClockValue = 3579545;
        static int FMClockValue = 7670454;
        static int samplingBuffer = 512;
        static short[] frames = new short[samplingBuffer * 2];
        MDSound.MDSound mds = new MDSound.MDSound(SamplingRate, samplingBuffer, FMClockValue, PSGClockValue);
        static private byte[] srcBuf = null;

        public Form1()
        {

            InitializeComponent();
            
        }

        private void btnRef_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter =
                "VGMファイル(*.vgm)|*.vgm";
            ofd.Title = "ファイルを選択してください";
            ofd.RestoreDirectory = true;
            ofd.CheckPathExists = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                tbFile.Text = ofd.FileName;
            }
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {

            try
            {

                srcBuf = File.ReadAllBytes(tbFile.Text);

            }
            catch
            {

                MessageBox.Show("ファイルの読み込みに失敗しました。");
                return;

            }

            btnPlay.Enabled = false;
            btnStop.Enabled = true;

            sdl = new SdlDotNet.Audio.AudioStream(SamplingRate, AudioFormat.Signed16Little, SoundChannel.Stereo, (short)samplingBuffer, new AudioCallback(callback), null);
            sdl.Paused = false;

            Thread thread = new Thread(new ThreadStart(FromVGM));
            thread.Start();



        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (sdl != null) sdl.Paused = true;
            btnPlay.Enabled = true;
            btnStop.Enabled = false;
        }

        private void callback(IntPtr userData, IntPtr stream, int len)
        {
            int i;

            int[][] buf= mds.Update();

            for (i = 0; i < len / 4; i++)
            {
                frames[i * 2 + 0] = (short)buf[0][i];
                frames[i * 2 + 1] = (short)buf[1][i];
            }


            System.Runtime.InteropServices.Marshal.Copy(frames, 0, stream, len / 2);
        }

        private void FromVGM()
        {
            //ヘッダーを読み込めるサイズをもっているかチェック
            if (srcBuf.Length < 0x40)
            {
                return;
            }

            //ヘッダーから情報取得

            uint vgm = getLE32(0x00);
            if (vgm != 0x206d6756)
            {
                return;
            }

            uint version = getLE32(0x08);
            if (version < 0x0150)
            {
                return;
            }

            uint eof = getLE32(0x04);

            uint vgmDataOffset = getLE32(0x34);
            if (vgmDataOffset == 0)
            {
                vgmDataOffset = 0x40;
            }
            else
            {
                vgmDataOffset += 0x34;
            }


            UInt32 adr = vgmDataOffset;
            byte[] YM2203RegMap = new byte[0x100]; //OPN
            byte[] YM2612RegMap = new byte[0x200]; //OPN2

            byte p = 0;
            byte rAdr = 0;
            byte rDat = 0;
            int w = 0;

            while (adr < srcBuf.Length || adr < eof)
            {
                byte cmd = srcBuf[adr];
                switch (cmd)
                {
                    case 0x4f: //GG PSG
                    case 0x50: //PSG
                        mds.Write(srcBuf[adr + 1]);
                        adr += 2;
                        break;
                    case 0x52: //YM2612 Port0
                    case 0x53: //YM2612 Port1
                        p = (byte)((cmd == 0x52) ? 0 : 1);
                        rAdr = srcBuf[adr + 1];
                        rDat = srcBuf[adr + 2];
                        YM2612RegMap[rAdr + p * 0x100] = rDat;
                        adr += 3;
                        mds.Write(p, rAdr, rDat);

                        break;
                    case 0x55: //YM2203
                        adr += 3;

                        break;
                    case 0x61: //Wait n samples
                        adr ++;
                        w += (int)getLE16(adr);
                        adr += 2;
                        //SdlDotNet.Core.Timer.DelayTicks(1);
                        break;
                    case 0x62: //Wait 735 samples
                        adr++;
                        w += 735;
                        break;
                    case 0x63: //Wait 882 samples
                        adr++;
                        w += 882;
                        break;
                    case 0x64: //override length of 0x62/0x63
                        //SdlDotNet.Core.Timer.DelayTicks(1);
                        adr += 4;
                        break;
                    case 0x66: //end of sound data
                        adr = (uint)srcBuf.Length;
                        SdlDotNet.Core.Timer.DelayTicks(1);
                        break;
                    case 0x67: //data block
                        adr += getLE32(adr + 3) + 7;
                        break;
                    case 0x70: //Wait 1 sample
                    case 0x71: //Wait 2 sample
                    case 0x72: //Wait 3 sample
                    case 0x73: //Wait 4 sample
                    case 0x74: //Wait 5 sample
                    case 0x75: //Wait 6 sample
                    case 0x76: //Wait 7 sample
                    case 0x77: //Wait 8 sample
                    case 0x78: //Wait 9 sample
                    case 0x79: //Wait 10 sample
                    case 0x7a: //Wait 11 sample
                    case 0x7b: //Wait 12 sample
                    case 0x7c: //Wait 13 sample
                    case 0x7d: //Wait 14 sample
                    case 0x7e: //Wait 15 sample
                    case 0x7f: //Wait 16 sample
                        w += cmd - 0x6f;
                        adr++;
                        break;
                    case 0x80: //Write adr2A and Wait 0 sample
                    case 0x81: //Write adr2A and Wait 1 sample
                    case 0x82: //Write adr2A and Wait 2 sample
                    case 0x83: //Write adr2A and Wait 3 sample
                    case 0x84: //Write adr2A and Wait 4 sample
                    case 0x85: //Write adr2A and Wait 5 sample
                    case 0x86: //Write adr2A and Wait 6 sample
                    case 0x87: //Write adr2A and Wait 7 sample
                    case 0x88: //Write adr2A and Wait 8 sample
                    case 0x89: //Write adr2A and Wait 9 sample
                    case 0x8a: //Write adr2A and Wait 10 sample
                    case 0x8b: //Write adr2A and Wait 11 sample
                    case 0x8c: //Write adr2A and Wait 12 sample
                    case 0x8d: //Write adr2A and Wait 13 sample
                    case 0x8e: //Write adr2A and Wait 14 sample
                    case 0x8f: //Write adr2A and Wait 15 sample
                        w += cmd - 0x7f;
                        adr++;
                        break;
                    case 0xe0: //seek to offset in PCM data bank
                        adr += 5;
                        break;
                    default:
                        //わからんコマンド
                        Console.WriteLine("{0:X}", srcBuf[adr]);
                        return;
                }
                while (w > 80)
                {
                    SdlDotNet.Core.Timer.DelayTicks(1);
                    w -= 80;
                }
                if (sdl.Paused)
                {
                    break;
                }
            }

            SdlDotNet.Core.Timer.DelayTicks(10);

            try
            {
                this.Invoke((Action)(delegate () { btnStop_Click(null, null); }));
            }
            catch { }
           
            return;
        }

        private static UInt32 getLE16(UInt32 adr)
        {
            UInt32 dat;
            dat = (UInt32)srcBuf[adr] + (UInt32)srcBuf[adr + 1] * 0x100;

            return dat;
        }

        private static UInt32 getLE32(UInt32 adr)
        {
            UInt32 dat;
            dat = (UInt32)srcBuf[adr] + (UInt32)srcBuf[adr + 1] * 0x100 + (UInt32)srcBuf[adr + 2] * 0x10000 + (UInt32)srcBuf[adr + 3] * 0x1000000;

            return dat;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            btnStop_Click(null, null);
        }
    }
}