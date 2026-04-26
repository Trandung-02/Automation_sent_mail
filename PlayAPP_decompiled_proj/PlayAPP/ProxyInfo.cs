namespace PlayAPP;

public class ProxyInfo
{
	public string Server { get; set; }

	/// <summary>Chuỗi truyền trực tiếp cho tham số --proxy-server của Chrome.</summary>
	public string ProxyServerArg { get; set; }

	public string Username { get; set; }

	public string Password { get; set; }

	/// <summary>Dòng gốc host:port hoặc host:port:user:pass — dùng cho log và so khớp proxy.</summary>
	public string RawLineForRuntime { get; set; }
}
