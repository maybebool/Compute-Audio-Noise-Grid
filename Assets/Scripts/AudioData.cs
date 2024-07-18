using UnityEngine;

public class SpectrumBandData {
    public float[] FrequencyBand { get; } = new float[8];
    public float[] BandBuffer { get; } = new float[8];
    public float[] BufferReduction { get; } = new float[8];
    public float[] FrequencyBandHighest { get; } = new float[8];
}

[RequireComponent(typeof(AudioSource))]
public class AudioData : MonoBehaviour {
    [SerializeField] private int bandCount = 8;
    [HideInInspector] public float[] audioBand;
    [HideInInspector] public float[] audioBandBuffer;
    [HideInInspector] public float amplitude;
    [HideInInspector] public float amplitudeBuffer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip audioClip;
    private float[] _samples = new float[512];
    private float _amplitudeHighest;
    private float _audioProfile;
    private SpectrumBandData _spectrumBandData = new();
    
    private const float UpperThreshold = 10;
    private void Start() {
        _audioProfile = 0.5f;
        audioBand = new float[bandCount];
        audioBandBuffer = new float[bandCount];
        AudioProfile(_audioProfile);
        audioSource.clip = audioClip;
        audioSource.Play();
    }


    private void Update() {
        if (audioSource.clip == null) return;
        GetAudioSpectrumData();
        GenerateFrequencyFilters();
        CalculateBandBuffer();
        GenerateAudioBands();
        GetAmplitude();
    }
    
    private void AudioProfile(float audioProfile) {
        for (int i = 0; i < bandCount; i++) {
            _spectrumBandData.FrequencyBandHighest[i] = audioProfile;
        }
    }

    private void GetAudioSpectrumData() {
        audioSource.GetSpectrumData(_samples, 0, FFTWindow.BlackmanHarris);
    }
    
    private void GenerateFrequencyFilters() {
        var count = 0;
        for (int i = 0; i < bandCount; i++) {
            var sampleCount = (int)Mathf.Pow(2, i) * 2;
            if (i == 7) {
                sampleCount += 2;
            }
            _spectrumBandData.FrequencyBand[i] = CalculateSampleAverage(i, count, sampleCount) * 10;
            count += sampleCount;
        }
    }
    
    private float CalculateSampleAverage(int i, int start, int sampleCount) {
        float total = 0;
        for (int j = start; j < start + sampleCount; j++) {
            total += (_samples[j] + _samples[j]) * (j + 1);
        }
        return total / sampleCount;
    }
    
    private void CalculateBandBuffer() {
        for (int i = 0; i < bandCount; ++i) {
            var frequencyBand = _spectrumBandData.FrequencyBand[i];
            var bandBuffer = _spectrumBandData.BandBuffer[i];

            if (frequencyBand > bandBuffer) {
                _spectrumBandData.BandBuffer[i] = frequencyBand;
            }

            if (!(frequencyBand < bandBuffer) || !(frequencyBand > 0)) continue;
            var bufferReduction = (bandBuffer - frequencyBand) / bandCount;
            _spectrumBandData.BufferReduction[i] = bufferReduction;
            _spectrumBandData.BandBuffer[i] -= bufferReduction;
        }
    }
    
    private void GenerateAudioBands() {
        for (int i = 0; i < bandCount; i++) {
            UpdateHighestFrequency(i);
            audioBand[i] = CalculateBand(i, _spectrumBandData.FrequencyBand);
            audioBandBuffer[i] = CalculateBand(i, _spectrumBandData.BandBuffer);
        }
    }
    
    
    private void GetAmplitude() {
        float currentAmplitude = 0;
        float currentAmplitudeBuffer = 0;
        for (int i = 0; i < bandCount; i++) {
            currentAmplitude += audioBand[i];
            currentAmplitudeBuffer += audioBandBuffer[i];
        }

        if (currentAmplitude > _amplitudeHighest) {
            _amplitudeHighest = currentAmplitude;
        }

        amplitude = currentAmplitude / _amplitudeHighest;
        amplitudeBuffer = currentAmplitudeBuffer / _amplitudeHighest;
    }
    

    private void UpdateHighestFrequency(int i) {
        if (_spectrumBandData.FrequencyBand[i] > _spectrumBandData.FrequencyBandHighest[i]) {
            _spectrumBandData.FrequencyBandHighest[i] = _spectrumBandData.FrequencyBand[i];
        }
    }

    private float CalculateBand(int i, float[] band) {
        return Mathf.Clamp((band[i] / _spectrumBandData.FrequencyBandHighest[i]), 0, 1);
    }
    
    
}