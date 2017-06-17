# Guardian Angel

<h2>Guardian Angel is the complete Anti-Ransomware Tool:</h2>
<ul style="list-style-type:disc">
   <li>Completely free for use</li>
        <li>Real-Time analysis of suspicious activities in your OneDrive files</li>
        <li>Keeps your files safe from encryption</li>
         <li>Non-intrusive and easy to use</li>
         </ul>

The project consists of two parts:

* An [Azure Function](https://azure.microsoft.com/services/functions/) definition that handles the processing of webhook notifications and the resulting work from those notifications
* An ASP.NET MVC application that activates and deactivated the Guardian Angel Robot for a signed in user.

In this scenario, the benefit of using Azure Function is that the load is required by the data robot is dynamic and hard to predict.
Instead of scaling out an entire web application to handle the load, Azure Functions can scale dynamically based on the load required at any given time.
This provides a cost-savings measure for hosting the application while still ensuring high performance results.

## Getting Started

Just go to out website and get your precious OneDrive files secures from ransomwares: 

To get started with the sample, you need to complete the following steps:


**Note:** OneDrive webhooks can take up to 5 minutes to be delivered, depending on load and other conditions.
As a result, requests in the workbook may take a few minutes to be updated with real data.

## Related references

For more information about Microsoft Graph API, see [Microsoft Graph](https://graph.microsoft.com).

## License

See [License](LICENSE.txt) for the license agreement convering this sample code.
