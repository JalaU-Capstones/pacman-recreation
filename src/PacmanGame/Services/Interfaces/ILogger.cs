using System;

namespace PacmanGame.Services.Interfaces;

public interface ILogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message);
    void Error(string message, Exception ex);
    void Debug(string message);
}
