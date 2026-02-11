using PacmanGame.Server;

var server = new RelayServer();
server.Start();

Console.WriteLine("Pac-Man Relay Server started. Press any key to exit.");
Console.ReadKey();

server.Stop();
