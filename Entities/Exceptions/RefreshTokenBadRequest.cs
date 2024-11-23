namespace Entities.Exceptions;

public class RefreshTokenBadRequest() : BadRequestException(
    "Invalid client request. The tokenDto has some invalid values.")
{ }
