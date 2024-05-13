using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectWaveButton : MonoBehaviour
{
    [SerializeField] TMPro.TMP_Text _title;
    AWaveData _wave;

    public void Init(AWaveData wave)
    {
        _wave = wave;
        _title.text = _wave.name;
    }

    public void SelectWave()
    {
        WaveView waveView = UIManager.instance.GetView<WaveView>(ViewType.Wave);
        waveView.OnWaveSelected.Invoke(_wave);
    }
}