using System.Runtime.CompilerServices;

namespace Refresher.Exceptions;

public class FTPConnectionFailureException() : Exception("Could not connect to the FTP server.");