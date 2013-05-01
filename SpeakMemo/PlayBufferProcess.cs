using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Xna.Framework.Audio;

namespace SpeakMemo
{
    public class PlayBufferProcess
    {
        //add by yy 2013-4-7
        readonly int REQUEST_DATA_MAX_LEN = 384;
        readonly int NUM_CHANNELS = 1;
        readonly int SAMPLE = 1;
        readonly int SINE_TABLE_LENGTH = 12;
        readonly int SAMPLE_MAX = (1 << 15);
        readonly int TABLE_JUMP_HIGH = 2;
        readonly int TABLE_JUMP_LOW = 2;
        public static int BYTES_PER_FRAME;
        Byte[] bufferPlay;
        Int16[] playBufferUint16;
        uint bufferPlayLength;
        int bitSinIndex = 0;
        Int16[] sineTable;
        UInt16 dataBits = 0;
        int dataBitIndex = 0;
        int packetDataIndex = 0;

        DynamicSoundEffectInstance playback;

        public PlayBufferProcess()
        {
            playback = new DynamicSoundEffectInstance(Microphone.Default.SampleRate, AudioChannels.Mono);
            InitVars();
        }
        //初始化变量的值 add by yy
        private void InitVars()
        {
            bufferPlay = new byte[REQUEST_DATA_MAX_LEN + 8];
            playBufferUint16 = new Int16[1024];
            sineTable = new Int16[SINE_TABLE_LENGTH];
            BYTES_PER_FRAME = NUM_CHANNELS * SAMPLE;
            bufferPlay[0] = 0xff;
            bufferPlay[1] = 0xff;
            bufferPlay[2] = 0xff;
            bufferPlay[3] = 0xff;
            bufferPlay[4] = 0xff;
            bufferPlay[5] = 0xff;
            bufferPlay[6] = 0xff;
            bufferPlay[7] = 0xff;
            bufferPlay[8] = 0xff;
            bufferPlay[9] = 0xff;
            bufferPlay[10] = 0xff;
            bufferPlay[11] = 0x00;
            for (int i = 0; i < SINE_TABLE_LENGTH; ++i)
            {
                sineTable[i] = (Int16)(0 - (Int16)(Math.Sin((float)(i * 2 * 3.14159 / SINE_TABLE_LENGTH)) * (1 << 15)));
            }
        }
        //处理data数据，增加头部和CRC校验，放到bufferPlay中，进行播放。！！！！！！！！！！！！！
        private int ProducePacket(byte[] data, uint length)
        {
            int crc;
            if (length > REQUEST_DATA_MAX_LEN)
            {
                return -1;
            }
            bufferPlay[6 + 6] = (byte)(((data.Length + 2) >> 8) & 0xff);
            //bufferPlay[(6 + 6) * 2+1] = BitConverter.GetBytes((UInt16)(((data.Length + 2) >> 8) & 0xff))[0];
            bufferPlay[7 + 6] = (byte)((length + 2) & 0xff);
            //bufferPlay[(7 + 6) * 2+1] = BitConverter.GetBytes((UInt16)((length + 2) & 0xff))[0];
            for (int i = 0; i < length; i++)
            {
                bufferPlay[i + 8 + 6] = data[i];
            }
            //crc = CRC16(Common.BytesArrayToUint16Array(data), length);
            crc = CRC16(data, length);
            bufferPlay[8 + 6 + length] = (byte)(crc >> 8);
            bufferPlay[8 + 6 + length + 1] = (byte)(crc);
            bufferPlayLength = 8 + 6 + length + 2;
            bufferPlayLength++;
            fillTxBuffer();
            return 0;
        }
        public void Play(byte[] data, uint length)
        {
            ProducePacket(data,length);
            playback.SubmitBuffer(Common.Uint16ArrayToBytesArray(playBufferUint16));
            //byte[] testdata = new byte[254];
            //for (int i = 0; i < testdata.Length; i++)
            //{
            //    testdata[i] = (byte)i;
            //}
            //ProducePacket(testdata, 254);
            //playback.SubmitBuffer(Common.Uint16ArrayToBytesArray(playBufferUint16));
            //playback.SubmitBuffer(testdata);
            playback.Play();
        }
        private int CRC16(byte[] b, uint length)
        {
            int i, j;
            UInt16 CRC = 0xFFFF;

            for (i = 0; i < length; i++)
            {
                CRC ^= b[i];
                for (j = 0; j < 8; j++)
                {
                    if (Convert.ToBoolean(CRC & 0x0001))
                    {
                        CRC = (UInt16)((CRC >> 1) ^ 0xA001);
                    }
                    else
                    {
                        CRC >>= 1;
                    }
                }
            }

            return CRC;
        }
        private void fillTxBuffer()
        {

            //byte[] sample = new byte[length];
            for (int i = 0; i < playBufferUint16.Length; i += BYTES_PER_FRAME)
            {
                playBufferUint16[i] = sineTable[bitSinIndex];
                //sample[i*2] = BitConverter.GetBytes(sineTable[bitSinIndex])[1];
                //sample[i * 2+1] = BitConverter.GetBytes(sineTable[bitSinIndex])[0];
                if (bufferPlayLength != 0)
                {
                    bitSinIndex += Convert.ToBoolean(dataBits & 1) ? TABLE_JUMP_HIGH : TABLE_JUMP_LOW;
                    if (bitSinIndex == SINE_TABLE_LENGTH)
                        bitSinIndex = 0;
                    if (bitSinIndex == 0)
                    {
                        dataBitIndex++;
                        if (dataBitIndex == 8)
                        {
                            dataBitIndex = 0;
                            if (dataBitIndex == 0)
                            {
                                packetDataIndex++;
                                if (packetDataIndex == bufferPlayLength)
                                {
                                    packetDataIndex = 0;
                                    bufferPlayLength = 0;
                                }
                                else
                                {
                                    dataBits = bufferPlay[packetDataIndex];
                                }
                            }
                        }
                        else
                        {
                            dataBits >>= 1;
                        }
                    }

                }
            }
            //return sample;
        } 
    }
}
