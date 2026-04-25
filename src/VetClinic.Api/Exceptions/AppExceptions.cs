namespace VetClinic.Api.Exceptions;

public class NotFoundException(string message) : Exception(message);
public class BusinessException(string message) : Exception(message);