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
using System.Collections.Generic;

namespace SpeakMemo
{
    enum FSKRecState
    {
        LGWaitSync=0,
        LGSyncHead,
        LGDataBits,
        LGSyncTail,
    }
    struct LGFSKDemodulator
    {
        public FSKRecState state;
        public uint syncHeadCount;
	    public uint bitPosition;
        public uint bytePosition;
        public uint lastNsInterval;
        public byte[] packetBuffer;
        public byte bits;
        public byte enable;
        //public LGFSKDemodulator()
        //{
        //    packetBuffer = new byte[512];
        //    enable = 0;
        //    state = FSKRecState.LGWaitSync;
        //    syncHeadCount = 0;
        //    bitPosition = 0;
        //    bytePosition = 0;
        //    lastNsInterval = 0;
        //    bits = 0;
        //}
    };
    struct PulseAnalyzer
    {
        public int lastFrame;
        public int lastEdgeSign;
        public uint lastEdgeWidth;
        public int edgeSign;
        public int edgeDiff;
        public uint edgeWidth;
        public uint plateauWidth;
    };

    
    public class RecordBufferProcess
    {
#if SAMPLE_MODE_16
readonly int FREQ_LOW=2000;
readonly int FREQ_HIGH=4000;
readonly int FREQ_SYNC_HEAD=1000;
readonly int FREQ_SYNC_TAIL=500;
readonly int SAMPLE_RATE=16000;
int SAMPLES_TO_NS(int __samples__)
        {
        return (__samples__*62500)； //(((UInt64)(__samples__) * 1000000000) / SAMPLE_RATE)
        }
#else
readonly int FREQ_LOW=3675;
readonly int FREQ_HIGH=7350;
readonly int FREQ_SYNC_HEAD=1260;
readonly int FREQ_SYNC_TAIL=630;
readonly int SAMPLE_RATE=44100;
readonly int EDGE_DIFF_THRESHOLD=(8192<<1);
readonly int EDGE_SLOPE_THRESHOLD=(256<<2);
readonly int EDGE_MAX_WIDTH=6;
readonly int IDLE_CHECK_PERIOD = 100;
 int WL_HIGH;
 int WL_LOW;
 int HWL_HIGH;
 int HWL_LOW;
 int WL_HIGH_MAX;
 int WL_HIGH_MIN;
 int WL_LOW_MAX;
 int WL_LOW_MIN;

 int WL_SYNC_HEAD;
 int WL_SYNC_TAIL;
 int WL_SYNC_HEAD_MAX;
 int WL_SYNC_HEAD_MIN;
 int WL_SYNC_TAIL_MAX;
 int WL_SYNC_TAIL_MIN;
 PulseAnalyzer gPulse; 
uint SAMPLES_TO_NS(uint __samples__)
{
    return (__samples__*22676); //(((UInt64)(__samples__) * 1000000000) / SAMPLE_RATE)
}
#endif

        Microphone microphone;
        // Used for storing captured buffers
        List<byte[]> memoBufferCollection = new List<byte[]>();
        private LGFSKDemodulator gLGFSK ;
        public RecordBufferProcess()
        {
            microphone = Microphone.Default;
            microphone.BufferReady += OnMicrophoneBufferReady;
            InitVars();
        }
        public byte[] GetFinalData()
        {
            return gLGFSK.packetBuffer;
        }
        private void OnMicrophoneBufferReady(object sender, EventArgs args)
        {
            // Get buffer from microphone and add to collection
            byte[] buffer = new byte[microphone.GetSampleSizeInBytes(microphone.BufferDuration)];
            int bytesReturned = microphone.GetData(buffer);
            memoBufferCollection.Add(buffer);
        }
        //分析reccord的data数据，放入gLGFSK的packetBuffer中，可以拷贝出来供上层处理。！！！！！！！！！！！！！
        private void AnalyzeRxBuffer(byte[] data, int length)
        {
            if (gLGFSK.enable == 0)
            {
                return;
            }
            //PulseAnalyzer data = gPulse;
            //SAMPLE* sample = (SAMPLE*)buffer;
            int lastFrame = gPulse.lastFrame;
            uint idleInterval = gPulse.plateauWidth + gPulse.lastEdgeWidth + gPulse.edgeWidth;
            for (int i = 0; i < length/2; i += PlayBufferProcess.BYTES_PER_FRAME)
            {
                //if (i%32==0) {
                //    __android_log_print(ANDROID_LOG_INFO, "AudioCOM", "%d,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d",*sample,*(sample+1),*(sample+2),*(sample+3),*(sample+4),*(sample+5),*(sample+6),*(sample+7),*(sample+8),*(sample+9),*(sample+10),*(sample+11),*(sample+12),*(sample+13),*(sample+14),*(sample+15));
                //}
                ///*
                int thisFrame;
                if (BitConverter.ToInt16(data,i*2) > -1600)
                {
                    thisFrame = 30000;
                }
                else
                {
                    thisFrame = -30000;
                }
                //*/

                //int thisFrame = *sample;
                int diff = thisFrame - lastFrame;

                int sign = 0;
                if (diff > EDGE_SLOPE_THRESHOLD)
                {
                    // Signal is rising
                    sign = 1;
                }
                else if (-diff > EDGE_SLOPE_THRESHOLD)
                {
                    // Signal is falling
                    sign = -1;
                }

                // If the signal has changed direction or the edge detector has gone on for too long,
                //  then close out the current edge detection phase
                if (gPulse.edgeSign != sign || (Convert.ToBoolean(gPulse.edgeSign) && gPulse.edgeWidth + 1 > EDGE_MAX_WIDTH))
                {
                    if (Math.Abs(gPulse.edgeDiff) > EDGE_DIFF_THRESHOLD && gPulse.lastEdgeSign != gPulse.edgeSign)
                    {
                        // The edge is significant
                        RevEdge(gPulse.edgeDiff, gPulse.edgeWidth, gPulse.plateauWidth + gPulse.edgeWidth);

                        // Save the edge
                        gPulse.lastEdgeSign = gPulse.edgeSign;
                        gPulse.lastEdgeWidth = gPulse.edgeWidth;

                        // Reset the plateau
                        gPulse.plateauWidth = 0;
                        idleInterval = gPulse.edgeWidth;
                    }
                    else
                    {
                        // The edge is rejected; add the edge gPulse to the plateau
                        gPulse.plateauWidth += gPulse.edgeWidth;
                    }
                    gPulse.edgeSign = sign;
                    gPulse.edgeWidth = 0;
                    gPulse.edgeDiff = 0;
                }

                if (Convert.ToBoolean(gPulse.edgeSign))
                {
                    // Sample may be part of an edge
                    gPulse.edgeWidth++;
                    gPulse.edgeDiff += diff;
                }
                else
                {
                    // Sample is part of a plateau
                    gPulse.plateauWidth++;
                }
                idleInterval++;
                lastFrame = thisFrame;


                //if ( (idleInterval % IDLE_CHECK_PERIOD) == 0 ){
                //[self idle:idleInterval];
                //initRxStructure();
                //}

            }
            gPulse.lastFrame = lastFrame;
        }
         //初始化变量的值 add by yy
        private void InitVars()
        {

            gLGFSK.packetBuffer = new byte[512]; ;
            WL_HIGH=(1000000000 / FREQ_HIGH);
            WL_LOW=(1000000000 / FREQ_LOW);
            HWL_HIGH=(500000000 / FREQ_HIGH);
            HWL_LOW=(500000000 / FREQ_LOW);
            WL_HIGH_MAX=(WL_HIGH*135/100);
            WL_HIGH_MIN=(WL_HIGH*65/100);
            WL_LOW_MAX=(WL_LOW*120/100);
            WL_LOW_MIN=(WL_LOW*80/100);

            WL_SYNC_HEAD=(1000000000 / FREQ_SYNC_HEAD);
            WL_SYNC_TAIL=(1000000000 / FREQ_SYNC_TAIL);
            WL_SYNC_HEAD_MAX=(WL_SYNC_HEAD*110/100);
            WL_SYNC_HEAD_MIN=(WL_SYNC_HEAD*90/100);
            WL_SYNC_TAIL_MAX=(WL_SYNC_TAIL*110/100);
            WL_SYNC_TAIL_MIN = (WL_SYNC_TAIL * 90 / 100);
        }
        public void StartRecord()
        {
            if (microphone.State == MicrophoneState.Stopped)
            {
                // Clear the collection for storing buffers
                memoBufferCollection.Clear();

                // Stop any playback in progress (not really necessary, but polite I guess)
                //playback.Stop();

                // Start recording
                microphone.Start();
            }
        }
        public void StopRecord()
        {
            // Get the last partial buffer
            int sampleSize = microphone.GetSampleSizeInBytes(microphone.BufferDuration);
            byte[] extraBuffer = new byte[sampleSize];
            int extraBytes = microphone.GetData(extraBuffer);
            memoBufferCollection.Add(extraBuffer);
            // Stop recording
            microphone.Stop();
            byte[] temp = Common.ByteArrayListToByteArray(memoBufferCollection);
            gLGFSK.enable = 1;
            AnalyzeRxBuffer(temp, temp.Length);
        }
        private void RevEdge(int height, uint width, uint interval)
        {
            uint nsInterval = SAMPLES_TO_NS(interval);
            //LGFSKDemodulator LGFSK = gLGFSK;
            if (gLGFSK.enable==0) {
                return;
            }
            //LOGI("LGState %d,edge coming,height %d,width %d,interval %d",LGFSK.state,height,width,nsInterval);
            if (height<0) {//just need positive edge now
                gLGFSK.lastNsInterval=nsInterval;
                return;
            }else{
                nsInterval=nsInterval+gLGFSK.lastNsInterval;
                gLGFSK.lastNsInterval = nsInterval;
            }
            switch (gLGFSK.state) {
                case FSKRecState.LGWaitSync:
                    if (nsInterval>WL_SYNC_HEAD_MIN && nsInterval<WL_SYNC_HEAD_MAX) {
                        gLGFSK.state = FSKRecState.LGSyncHead;
                        gLGFSK.syncHeadCount++;
                    }
                    break;
                case FSKRecState.LGSyncHead:
                    if (nsInterval>WL_SYNC_HEAD_MIN && nsInterval<WL_SYNC_HEAD_MAX) {
                        gLGFSK.syncHeadCount++;
                    }else if(gLGFSK.syncHeadCount>2){      //sync head plus 8 ->4
                        if (nsInterval>WL_HIGH_MIN && nsInterval<WL_HIGH_MAX) {
                            gLGFSK.state = FSKRecState.LGDataBits;
                            revDataBit(1);
                        }else if (nsInterval>WL_LOW_MIN && nsInterval<WL_LOW_MAX){
                            gLGFSK.state = FSKRecState.LGDataBits;
                            revDataBit(0);
                        }
                    }else{
                        gLGFSK.state = FSKRecState.LGWaitSync;
                        gLGFSK.syncHeadCount=0;
                    }
                    break;
                case FSKRecState.LGDataBits:
                    if (nsInterval>WL_HIGH_MIN && nsInterval<WL_HIGH_MAX) {
                        revDataBit(1);
                    }else if (nsInterval>WL_LOW_MIN && nsInterval<WL_LOW_MAX){
                        revDataBit(0);
                    }else if (nsInterval>WL_SYNC_TAIL_MIN && nsInterval<WL_SYNC_TAIL_MAX){
                        //take data out, tail plus 4 -> 2
                        //LOGI("take data success! bytesLen: %d, [0]:%02X  [n-4]: %02X [n-3]: %02X nsInterval %d",\
                        //     LGFSK->bytePosition,LGFSK->packetBuffer[0],LGFSK->packetBuffer[LGFSK->bytePosition-4],LGFSK->packetBuffer[LGFSK->bytePosition-3],nsInterval);
                        gLGFSK.enable=0;
                        byte cmd;
                        if (parseLGData(gLGFSK.packetBuffer, gLGFSK.bytePosition)==1) {
                            cmd=0;
                        }else{
                            cmd=2;  //crc error
                        }
                        //to do...
                        //write( gSocket[0], &cmd, 1 );
                        //initRxStructure();
                    }else{
                        //LOGI("take data fail! bytePosition %d, bytes:%02X %02X %02X %02X nsInterval %d",\
                        //     LGFSK.bytePosition,LGFSK.packetBuffer[0],LGFSK.packetBuffer[1],LGFSK.packetBuffer[2],LGFSK.packetBuffer[3],nsInterval);
                        gLGFSK.enable=0;
                        byte cmd=1;
                        //to do...
                        //write( gSocket[0], &cmd, 1 );
                        //initRxStructure();
                    }
                    break;
                default:
                    break;
            }
        }
        private void revDataBit(int isOne)
        {
            if (isOne == 1)
                gLGFSK.bits |= (byte)(1 << (int)gLGFSK.bitPosition);
            gLGFSK.bitPosition++;
            if (gLGFSK.bitPosition == 8)
            {
                gLGFSK.bitPosition = 0;
                gLGFSK.packetBuffer[gLGFSK.bytePosition] = gLGFSK.bits;
                gLGFSK.bytePosition++;
                gLGFSK.bits = 0;
            }
        }
        private int parseLGData(byte[] bytes, uint length)
        {
            //unsigned char* b=(unsigned char*)bytes;

            if (MAKEWORD(bytes[1], bytes[0]) != (length - 2))
            {
                return 0;
            }
            int crc = CRC16(LeftMoveNum(bytes,2), length - 4);
            if (crc != ((UInt16)(bytes[length - 2] << 8) + bytes[length - 1]))
            {//crc error
                return 0;
            }
            return 1;  
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
        private int MAKEWORD(UInt16 low, UInt16 high)
        {
            return (((high) << 8) | ((low) & 0xff));
        }
        private byte[] LeftMoveNum(byte[] b, int num)
        {
            byte[] result = new byte[b.Length];
            for (int i = 0; i < b.Length-num; i++)
            {
                result[i] = b[i + num];
            }
            return result;
        }
    }
}
