Imports System
Imports System.IO
Imports System.Text

Module Program

    Const SampleRate As Integer = 44100
    Const BitsPerSample As Short = 16
    Const NumChannels As Short = 1
    Const Volume As Double = 0.3
    Const NoteDurationSec As Double = 0.3
    Const PauseDurationSec As Double = 0.05

    Dim Chords As New Dictionary(Of Char, Integer()) From {
        {"0"c, {60, 64, 67}},
        {"1"c, {62, 65, 69}},
        {"2"c, {64, 67, 71}},
        {"3"c, {65, 69, 72}},
        {"4"c, {67, 71, 74}},
        {"5"c, {69, 72, 76}},
        {"6"c, {71, 74, 77}},
        {"7"c, {72, 76, 79}},
        {"8"c, {74, 77, 81}},
        {"9"c, {76, 79, 83}}
    }

    Dim Notes As New Dictionary(Of Char, Integer) From {
        {"A"c, 69}, {"B"c, 71}, {"C"c, 60}, {"D"c, 62},
        {"E"c, 64}, {"F"c, 65}, {"G"c, 67}, {"H"c, 70}
    }

    Sub Main()
        Console.ForegroundColor = ConsoleColor.Black
        Console.BackgroundColor = ConsoleColor.DarkYellow
        Console.Write("Nome do ficheiro .txt: ")
        Dim inputFile As String = Console.ReadLine().Trim()

        If Not File.Exists(inputFile) Then
            Console.WriteLine("Ficheiro não encontrado.")
            Return
        End If

        Dim content As String = File.ReadAllText(inputFile).ToUpper()
        Dim outputFile As String = Path.ChangeExtension(inputFile, ".wav")

        Using stream As New MemoryStream()
            For Each ch As Char In content
                If Chords.ContainsKey(ch) Then
                    AppendNote(stream, Chords(ch), NoteDurationSec)
                ElseIf Notes.ContainsKey(ch) Then
                    AppendNote(stream, {Notes(ch)}, NoteDurationSec)
                Else
                    Continue For
                End If
                AppendSilence(stream, PauseDurationSec)
            Next

            Dim audioData As Byte() = stream.ToArray()

            Using fout As New FileStream(outputFile, FileMode.Create)
                WriteWavHeader(fout, audioData.Length)
                fout.Write(audioData, 0, audioData.Length)
            End Using
        End Using

        Console.WriteLine($"✅ Ficheiro WAV gerado: {outputFile}")
    End Sub

    Sub AppendNote(ms As MemoryStream, midiNotes() As Integer, durationSec As Double)
        Dim samples As Integer = CInt(SampleRate * durationSec)
        For i As Integer = 0 To samples - 1
            Dim t As Double = i / SampleRate
            Dim sample As Double = 0
            For Each n In midiNotes
                sample += Math.Sin(2 * Math.PI * MidiToFreq(n) * t)
            Next
            sample = (sample / midiNotes.Length) * Volume
            Dim s As Short = CShort(sample * Short.MaxValue)
            ms.WriteByte(CByte(s And &HFF))
            ms.WriteByte(CByte((s >> 8) And &HFF))
        Next
    End Sub

    Sub AppendSilence(ms As MemoryStream, durationSec As Double)
        Dim samples As Integer = CInt(SampleRate * durationSec)
        For i As Integer = 0 To samples - 1
            ms.WriteByte(0)
            ms.WriteByte(0)
        Next
    End Sub

    Function MidiToFreq(midiNote As Integer) As Double
        Return 440.0 * Math.Pow(2, (midiNote - 69) / 12.0)
    End Function

    Sub WriteWavHeader(stream As Stream, dataLength As Integer)
        Dim byteRate As Integer = SampleRate * NumChannels * BitsPerSample \ 8
        Dim blockAlign As Short = NumChannels * BitsPerSample \ 8
        Dim chunkSize As Integer = 36 + dataLength

        Using writer As New BinaryWriter(stream, Encoding.ASCII, leaveOpen:=True)
            writer.Write(Encoding.ASCII.GetBytes("RIFF"))
            writer.Write(chunkSize)
            writer.Write(Encoding.ASCII.GetBytes("WAVE"))

            writer.Write(Encoding.ASCII.GetBytes("fmt "))
            writer.Write(16) ' Subchunk1Size
            writer.Write(CShort(1)) ' AudioFormat PCM
            writer.Write(NumChannels)
            writer.Write(SampleRate)
            writer.Write(byteRate)
            writer.Write(blockAlign)
            writer.Write(BitsPerSample)

            writer.Write(Encoding.ASCII.GetBytes("data"))
            writer.Write(dataLength)
        End Using
    End Sub

End Module
