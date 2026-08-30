function Initialize()
    print("=== RIS WEBHOOK LUA LOADED ===")
end

function OnStableStudy(studyId, tags, metadata)
    print("=== ORTHANC STABLE STUDY ===")
    print("Orthanc Study ID: " .. studyId)

    local studyInstanceUid = tags["StudyInstanceUID"]

    if studyInstanceUid == nil or studyInstanceUid == "" then
        print("StudyInstanceUID is missing")
        return
    end

    local body =
        '{"studyInstanceUid":"' ..
        studyInstanceUid ..
        '"}'

    local headers = {
        ["Content-Type"] = "application/json",
        ["X-Webhook-Secret"] = "dev-secret"
    }

    SetHttpTimeout(5)

    local response = HttpPost(
        "http://host.docker.internal:5171/webhooks/orthanc",
        body,
        headers)

    print("Webhook response: " .. tostring(response))
end