# Stock Tunnel Monitoring System

This application monitors the price of a stock and sends email alerts when the price crosses predefined upper or lower limits.

## Prerequisites

- .NET Core SDK (version X.X.X or higher)
- An SMTP server for sending emails
- MailKit library (automatically installed via NuGet package restore)

## Installation

1. Clone the repository or download the source code.

2. Open the project in your preferred integrated development environment (IDE).

3. Update the `appsettings.json` file with the necessary configuration for your SMTP server and email settings.

4. Build the project to ensure that all dependencies are resolved.

## Usage

1. Open a terminal or command prompt and navigate to the project directory.

2. Run the following command to start the application: `dotnet run <ticker> <limitUpper> <limitLower>`

    Replace `<ticker>` with the stock ticker symbol you want to monitor.
    Replace `<limitUpper>` with the upper limit price for the stock.
    Replace `<limitLower>` with the lower limit price for the stock.

3. The application will continuously monitor the stock price and display it on the console. If the price exceeds the defined limits, an email alert will be sent to the specified recipient.

4. To stop the application, press `Ctrl+C` in the terminal or command prompt.

## Configuration

The application uses the `appsettings.json` file for configuration. Update the following settings:

- `SmtpHost`: The SMTP server host name.
- `SmtpPorta`: The SMTP server port number.
- `SmtpUsuario`: The username for authenticating with the SMTP server.
- `SmtpSenha`: The password for authenticating with the SMTP server.
- `EmailDestino`: The recipient email address for receiving alerts.

## Dependencies

The project depends on the following NuGet packages:

- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Configuration.Json
- Newtonsoft.Json
- MailKit

