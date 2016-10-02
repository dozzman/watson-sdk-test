using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using IBM.Watson.DeveloperCloud.Services.Conversation.v1;
using IBM.Watson.DeveloperCloud.Services.TextToSpeech.v1;
using IBM.Watson.DeveloperCloud.Services.SpeechToText.v1;

public class Watson : MonoBehaviour {

	private Conversation m_Conversation = new Conversation();
	private string m_WorkspaceID = "aac8ea67-f3fb-467e-be4c-8dc5afa12ecb";
	private TextToSpeech m_TextToSpeech = new TextToSpeech();
	private SpeechToText m_SpeechToText = new SpeechToText();

	private AudioClip requestClip;
	private Context ctx;

	private const int READY = 0;
	private const int LISTENING = 1;
	private const int WORKING = 2;

	private int state;

	public Button button;
	public Text buttonText;

	void Start() {
		m_TextToSpeech.Voice = VoiceType.en_GB_Kate;
		SetState(READY);
	}

	void OnMessage(MessageResponse resp, string customData) {
		Debug.Log("resp = " + resp);
		ctx = resp.context;
		foreach(Intent i in resp.intents) {
			Debug.Log("intent: " + i.intent + ", confidence: " + i.confidence);
		}

		foreach(string output in resp.output.text) {
			Debug.Log("response: " + output);
			m_TextToSpeech.ToSpeech(output, HandleToSpeechCallback);

		}
	}

	void HandleToSpeechCallback (AudioClip clip) {
		PlayClip(clip);
	}

	void PlayClip(AudioClip clip) {
		GameObject audioObject = new GameObject();
		AudioSource audioSource = audioObject.AddComponent<AudioSource>();
		audioSource.clip = clip;
		audioSource.Play();
		Destroy(audioObject, clip.length);
		SetState(READY);
	}

	void BeginListening() {
		requestClip = Microphone.Start(null, false, 10, 44100);
	}

	void StopListening() {
		Microphone.End(null);
		m_SpeechToText.Recognize(requestClip, OnRecognize);
	}

	void OnRecognize(SpeechResultList resultList) {
		if (resultList.Results.Length > 0) {
			string request = resultList.Results[0].Alternatives[0].Transcript;
			Debug.Log("Understood: " + request);
			MessageRequest msgRequest = new MessageRequest();
			msgRequest.context = ctx;
			msgRequest.InputText = request;
			m_Conversation.Message(OnMessage, m_WorkspaceID, msgRequest);

		}
	}

	void SetState(int newState) {
		switch (newState) {
		case READY:
			state = READY;
			buttonText.text = "Waiting";
			button.interactable = true;
			break;

		case LISTENING:
			state = LISTENING;
			buttonText.text = "Listening";
			BeginListening();
			break;

		case WORKING:
			state = WORKING;
			button.interactable = false;
			buttonText.text = "Thinking";
			StopListening();
			break;
		}
	}

	public void ButtonPressed() {
		Debug.Log("Pressed button");
		switch (state) {
		case READY:
			SetState(LISTENING);
			break;

		case LISTENING:
			SetState(WORKING);
			break;
		}
	}
}
