using System;
public enum EventID
{
	OnSingleWorldCreated_Server,
	OnMultiWorldCreated_Server,


	OnSocketRecievedData,
	OnConnectionRecievedData,
	OnPlayerConnectedToServer_Server,
    OnPlayerConnectedToServer_Client,
	OnPlayerDisconnectedToServer_Server,
    OnJoinedServer,

	/// <summary>Called when server has fully loaded their game (Player is in their current scene)</summary>
	OnServerGameLoaded,

	OnNetworkUpdateStart_Server,
	OnNetworkUpdateEnd_Server,

    OnNetworkUpdate_Client,

	OnConsoleClose,
	OnConsoleOpen,

}
public class Event
{

	public Event(EventID eventID, object parameter)
    {
		this.eventID = eventID.ToString();
		this.parameter = parameter;
    }
	public Event(string eventID, object parameter)
    {
		this.eventID = eventID;
		this.parameter = parameter;
    }
	public Event(EventID eventID, object parameter, object caller)
    {
		this.eventID = eventID.ToString();
		this.parameter= parameter;
		this.caller = caller;
    }
	public Event(string eventID, object parameter, object caller)
	{
		this.eventID = eventID;
		this.parameter = parameter;
		this.caller = caller;
	}

	~Event() { }

	public string ID { get { return eventID; } }
	public EventID IDAsEvent 
	{ 
		get 
		{
			EventID id;
			Enum.TryParse(eventID, out id);
			return id;
		}
	}
	public object Parameter { get { return parameter; } }
	public object Caller { get { return caller; } }


	string eventID;
	object parameter;
	object caller;
};