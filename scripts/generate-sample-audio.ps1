$audioDir = "src/TentoUI.Docs/wwwroot/audio"
if (-not (Test-Path $audioDir)) {
    New-Item -ItemType Directory -Path $audioDir -Force | Out-Null
}

function Generate-Wav {
    param(
        [string]$filePath,
        [double]$durationSec,
        [object[]]$frequencies
    )
    $sampleRate = 22050
    $numSamples = [int]($sampleRate * $durationSec)
    $subChunk2Size = $numSamples * 2
    $chunkSize = 36 + $subChunk2Size

    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)

    # RIFF header
    $bw.Write([System.Text.Encoding]::ASCII.GetBytes("RIFF"))
    $bw.Write([int32]$chunkSize)
    $bw.Write([System.Text.Encoding]::ASCII.GetBytes("WAVE"))

    # fmt chunk
    $bw.Write([System.Text.Encoding]::ASCII.GetBytes("fmt "))
    $bw.Write([int32]16)
    $bw.Write([int16]1)
    $bw.Write([int16]1)
    $bw.Write([int32]$sampleRate)
    $bw.Write([int32]($sampleRate * 2))
    $bw.Write([int16]2)
    $bw.Write([int16]16)

    # data chunk
    $bw.Write([System.Text.Encoding]::ASCII.GetBytes("data"))
    $bw.Write([int32]$subChunk2Size)

    $stepDuration = $durationSec / $frequencies.Count
    for ($i = 0; $i -lt $numSamples; $i++) {
        $t = $i / $sampleRate
        $chordIdx = [Math]::Min([int]($t / $stepDuration), $frequencies.Count - 1)
        $freqs = $frequencies[$chordIdx]

        $sampleVal = 0.0
        foreach ($f in $freqs) {
            $sampleVal += [Math]::Sin(2.0 * [Math]::PI * $f * $t) * 0.25
            $sampleVal += [Math]::Sin(4.0 * [Math]::PI * $f * $t) * 0.08
        }

        # Envelope
        $localT = $t - ($chordIdx * $stepDuration)
        $attack = [Math]::Min(1.0, $localT / 0.04)
        $decay = [Math]::Min(1.0, ($stepDuration - $localT) / 0.04)
        $envelope = [Math]::Max(0.0, $attack * $decay)
        $sampleVal = $sampleVal * $envelope

        $intVal = [int]($sampleVal * 16000)
        if ($intVal -gt 32767) { $intVal = 32767 }
        if ($intVal -lt -32767) { $intVal = -32767 }
        $bw.Write([int16]$intVal)
    }

    $bw.Flush()
    [System.IO.File]::WriteAllBytes($filePath, $ms.ToArray())
    $bw.Close()
    $ms.Close()
}

Generate-Wav -filePath "$audioDir/synthwave.wav" -durationSec 8.0 -frequencies @(
    @(261.63, 329.63, 392.0),
    @(196.00, 246.94, 293.66),
    @(220.00, 261.63, 329.63),
    @(174.61, 220.00, 261.63)
)

Generate-Wav -filePath "$audioDir/speech-sample.wav" -durationSec 5.0 -frequencies @(
    @(320.0, 480.0),
    @(400.0, 600.0),
    @(350.0, 520.0),
    @(280.0, 420.0)
)

Write-Host "Generated sample audio files in $audioDir"
