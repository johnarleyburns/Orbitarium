# RT-Voice 2.5.2

Thank you for buying our asset "RT-Voice"! 
If you have any questions about this asset, send us an email at [assets@crosstales.com](mailto:assets@crosstales.com). 
Please don't forget to rate it or even better write a little review – it would be very much appreciated.

If you need the source code, consider upgrading to the PRO edition:

https://www.assetstore.unity3d.com/en/#!/content/41068



## Description
Have you ever wanted to make software for people with visual impairment or who have difficulties reading? 
Do you have lazy players who don't like to read too much? Or do you even want to test your game's voice dialogues without having to pay a voice actor yet? 
With RT-Voice this is very easily done – it's a major time saver!

RT-Voice uses the computer's (already implemented) TTS (text-to-speech) voices to turn the written lines into speech and dialogue at run-time! 
Therefore, all text in your game/app can be spoken out loud to the player.

And all of this without any intermediate steps: The transformation is instantaneous and simultaneous (if needed)!

Please read the "RTVoice-doc.pdf" and "RTVoice-api.pdf" for more details.



## Upgrade to new version
Follow this steps to upgrade your version of "RT-Voice":

1. Update "RT-Voice" to the latest version from the "Asset Store" => https://www.assetstore.unity3d.com/#!/content/48394
2. Inside your project in Unity, go to menu "File" => "New Scene"
3. Delete the "crosstales\RTVoice" folder from the Project-view
4. Import the latest version from the "Asset Store"



## Release notes

### 2.5.2
* TextFileSpeaker added
* DontDestroyOnLoad is now configurable in the settings (currently hidden)
* The Windows-TTS-Wrapper is now configurable in the settings (currently hidden)
* The macOS-TTS-command is now configurable in the settings (currently hidden)
* Settings are managed via CTPlayerPrefs
* Default as DLL (sources are inside the 'sources.zip')
* Code clean-up

### 2.5.1
* PlayMaker actions improved
* 32bit-mode for Windows changed
* Unity 5.5-ready

### 2.5.0
* Android support added!
* iOS support added!
* Support for 32-Bit Windows system voices
* Pitch added
* Namespaces extended
* Code improvements

### 2.4.3
* Editor integration improved
* Rename of all events to OnXY
* Marginal code changes

### 2.4.2
* 3rd party support (AC, UDEA and PlayMaker) improved
* New method to approximate the length of a speech: Speaker.ApproximateSpeechLength()
* Test-Drive added to the configuration window
* Callbacks are now on the Speaker-class
* Error-callback 'ErrorInfoEvent' added
* Documentation improved

### 2.4.1
* Configuration window and "Unity Preferences" added

### 2.4.0
* SpeechText added
* LiveSpeaker improved
* Automatically adds the neccessary RTVoice-prefabs to the current scene
* Update-checker added
* Playmaker actions improved
* RTVoiceTTSWrapper.exe rebuilded (is now 'AnyCPU' instead of 'x86') and malware report from [Metadefender](https://www.metadefender.com/) added
* VoiceProvider is now platform independent
* Demo for 'Adventure Creator' added (see folder '3rd party')
* Demo for 'Cinema Director' added (see folder '3rd party')
* Demo for 'Dialogue System' added (see folder '3rd party')
* Demo for 'LDC' added (see folder '3rd party')
* Demo for 'LipSync' added (see folder '3rd party')
* Demo for 'SLATE' added (see folder '3rd party')
* Demo for 'THE Dialogue Engine' added (see folder '3rd party')
* Code improvements
* Documentation updated (section "Additional voices")
* Minimal Unity version is now 5.2.1

### 2.3.1
* Code clean-up

### 2.3.0
* Generated audio can now be stored on a desired file path (see Wrapper-class -> 'OutputFile')
* Loudspeaker added: use 1-n synchronized loudspeakers for a single AudioSource origin.
* The Silence()-method works now with provided AudioSources
* Correct handling of AudioSource.Pause() and AudioSource.UnPause()
* SALSA-demo added (see folder '3rd party')
* Code improvements

### 2.2.1
* Playmaker actions updated

### 2.2.0
* Playmaker actions added

### 2.1.2
* Demo scenes improved
* Windows provider improved

### 2.1.1
* Multi-threading improved
* Demo scenes improved
* New callbacks added

### 2.1.0
* Sequencer added
* Demo scenes improved (with many 3D audio examples)
* Multi-threading added
* Better Unity Editor integration
* ExecuteInEditMode removed
* Timing for callbacks improved

### 2.0.0
* Various callbacks added
* Added visemes and phomenes on Windows
* Rate and volume control added
* Code clean-up

### 1.4.1
* Exit-handling of processes improved

### 1.4.0
* Standard edition created



## Contact

crosstales LLC
Weberstrasse 21
CH-8004 Zürich

[Homepage](http://www.crosstales.com/en/assets/rtvoice/)
[Email](mailto:assets@crosstales.com)
[AssetStore](https://www.assetstore.unity3d.com/#!/content/48394)
[Forum](http://forum.unity3d.com/threads/coming-soon-rt-voice.340046/)
[Documentation](http://www.crosstales.com/en/assets/rtvoice/RTVoice-doc.pdf)
[API](http://www.crosstales.com/en/assets/rtvoice/api)
[Windows-Demo](http://www.crosstales.com/en/assets/rtvoice/RTVoice_demo_win.zip)
[Mac-Demo](http://www.crosstales.com/en/assets/rtvoice/RTVoice_demo_mac.zip)

`Version: 26.10.2016 19:16`