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
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip audioClip;
    
    [HideInInspector] public float[] audioBand;
    [HideInInspector] public float[] audioBandBuffer;
    [HideInInspector] public float amplitude;
    [HideInInspector] public float amplitudeBuffer;
    
    private float[] _samples = new float[512];
    private float _amplitudeHighest;
    private float _audioProfile;
    private SpectrumBandData _spectrumBandData = new();
    
    // Hint: try to integrate this unused method in one of the methods
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
        GetAmplitudeBuffer();
    }

    /// <summary>
    /// Modifies the audio profile by setting the highest frequency band of the SpectrumBandData to the specified value.
    /// </summary>
    /// <param name="audioProfile">The value to set as the highest frequency band of the SpectrumBandData.</param>
    private void AudioProfile(float audioProfile) {
        for (int i = 0; i < bandCount; i++) {
            _spectrumBandData.FrequencyBandHighest[i] = audioProfile;
        }
    }

    /// <summary>
    /// Get the audio spectrum data by calling the `GetSpectrumData` method on the attached AudioSource component.
    /// </summary>
    private void GetAudioSpectrumData() {
        audioSource.GetSpectrumData(_samples, 0, FFTWindow.BlackmanHarris);
    }

    /// <summary>
    /// Generate the frequency filters for each frequency band based on the audio spectrum data.
    /// </summary>
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
    
    /// <summary>
    /// Calculate the average sample value for a specific frequency band.
    /// </summary>
    /// <param name="i">The index of the frequency band.</param>
    /// <param name="start">The start index of the sample values in the audio data.</param>
    /// <param name="sampleCount">The number of sample values to calculate the average from.</param>
    /// <returns>The average sample value for the specified frequency band.</returns>
    private float CalculateSampleAverage(int i, int start, int sampleCount) {
        float total = 0;
        for (int j = start; j < start + sampleCount; j++) {
            total += (_samples[j] + _samples[j]) * (j + 1);
        }
        return total / sampleCount;
    }
    
    /// <summary>
    /// Calculate the band buffer value for each frequency band based on the spectrum data.
    /// </summary>
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

    /// <summary>
    /// Generate the audio bands based on the frequency data.
    /// </summary>
    private void GenerateAudioBands() {
        for (int i = 0; i < bandCount; i++) {
            UpdateHighestFrequency(i);
            audioBand[i] = CalculateBand(i, _spectrumBandData.FrequencyBand);
            audioBandBuffer[i] = CalculateBand(i, _spectrumBandData.BandBuffer);
        }
    }
    
    /// <summary>
    /// Calculate the amplitude buffer based on the audio band values.
    /// </summary>
    private void GetAmplitudeBuffer() {
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
    
    /// <summary>
    /// Update the highest frequency value for a specific band in the given SpectrumBandData object.
    /// </summary>
    /// <param name="i">The index of the band to update.</param>
    private void UpdateHighestFrequency(int i) {
        if (_spectrumBandData.FrequencyBand[i] > _spectrumBandData.FrequencyBandHighest[i]) {
            _spectrumBandData.FrequencyBandHighest[i] = _spectrumBandData.FrequencyBand[i];
        }
    }

    /// <summary>
    /// Calculate the value for a specific frequency band based on the given band data.
    /// </summary>
    /// <param name="i">The index of the frequency band.</param>
    /// <param name="band">The array containing the band data.</param>
    /// <returns>The calculated value for the specified frequency band.</returns>
    private float CalculateBand(int i, float[] band) {
        return Mathf.Clamp((band[i] / _spectrumBandData.FrequencyBandHighest[i]), 0, 1);
    }
}