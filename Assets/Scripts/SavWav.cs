using System;
using System.IO;
using UnityEngine;

public static class SavWav
{
    public static byte[] GetWavBytes(AudioClip clip)
    {
        using (var stream = new MemoryStream())
        {
            // 写入 WAV 文件头
            Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            stream.Write(riff, 0, 4);
            Byte[] chunkSize = BitConverter.GetBytes(clip.samples * 2 + 36);
            stream.Write(chunkSize, 0, 4);
            Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            stream.Write(wave, 0, 4);
            Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            stream.Write(fmt, 0, 4);
            Byte[] subChunk1Size = BitConverter.GetBytes(16);
            stream.Write(subChunk1Size, 0, 4);
            UInt16 audioFormat = 1; // PCM
            stream.Write(BitConverter.GetBytes(audioFormat), 0, 2);
            UInt16 numChannels = (UInt16)clip.channels;
            stream.Write(BitConverter.GetBytes(numChannels), 0, 2);
            UInt32 sampleRate = (UInt32)clip.frequency;
            stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);
            UInt32 byteRate = sampleRate * numChannels * 2;
            stream.Write(BitConverter.GetBytes(byteRate), 0, 4);
            UInt16 blockAlign = (UInt16)(numChannels * 2);
            stream.Write(BitConverter.GetBytes(blockAlign), 0, 2);
            UInt16 bitsPerSample = 16;
            stream.Write(BitConverter.GetBytes(bitsPerSample), 0, 2);
            Byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
            stream.Write(dataString, 0, 4);
            Byte[] subChunk2Size = BitConverter.GetBytes(clip.samples * 2);
            stream.Write(subChunk2Size, 0, 4);

            // 写入音频实体数据
            float[] samples = new float[clip.samples];
            clip.GetData(samples, 0);
            Int16[] intData = new Int16[samples.Length];
            Byte[] bytesData = new Byte[samples.Length * 2];

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * 32767);
                Byte[] byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }
            stream.Write(bytesData, 0, bytesData.Length);
            return stream.ToArray();
        }
    }
}