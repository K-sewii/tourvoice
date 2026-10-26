namespace TourVoice.DAL.Entities;

public enum PlaceKind { Destination, Food }
public enum PlaceStatus { Draft, Published, Hidden }
public enum TranslationStatus { Draft, Reviewed, Generating, AudioReady, AudioFailed }
public enum TranslationSource { Human, Machine }
public enum UserRole { Customer, Admin }
public enum ListenSource { Mp3, DeviceVoice }
public enum UsageService { Translator, Speech }
public enum AccessSource { Web, QR }