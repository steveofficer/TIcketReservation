module HttpNotification

/// Deliver the payload to the provided url
let ``deliver notification`` (url : string) (messageId : System.Guid) (payload : obj) = async {
    let request = System.Net.HttpWebRequest.CreateHttp(url)
    request.Method <- "POST"
    request.ContentType <- "application/json"
    
    request.Headers.Add(sprintf "X-Message-Id: %s" (messageId.ToString()))
    request.Headers.Add(sprintf "X-Delivered-Message-Type: %s" (payload.GetType().Name))

    let content = payload |> Newtonsoft.Json.JsonConvert.SerializeObject |> System.Text.UTF8Encoding.UTF8.GetBytes
    
    use! body = request.GetRequestStreamAsync() |> Async.AwaitTask
    do! body.WriteAsync(content, 0, content.Length) |> Async.AwaitTask |> Async.Ignore
    body.Close()

    use! response = request.GetResponseAsync() |> Async.AwaitTask
    return()
}