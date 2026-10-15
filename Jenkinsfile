pipeline {
    agent { label 'windows' }
    options {
        skipDefaultCheckout(true)
        disableConcurrentBuilds()
        timeout(time: 20, unit: 'MINUTES')
        timestamps()
        buildDiscarder(logRotator(numToKeepStr: '20', artifactNumToKeepStr: '5'))
    }
    parameters {
        choice(name: 'SOURCE_BRANCH', choices: ['main', 'demo/build-failure', 'demo/test-failure'], description: 'Only main can deploy. Failure branches are for classroom demonstrations.')
        booleanParam(name: 'DEPLOY_TO_IIS', defaultValue: true, description: 'Requires IIS + Hosting Bundle + deployment permissions on this Windows agent.')
    }
    triggers { githubPush() }
    environment {
        DEPLOY_PATH = 'C:\\wwwroot\\webbanhang'
        VERIFY_URL = 'http://localhost/webbanhang/health'
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    }
    stages {
        stage('Checkout') {
            steps {
                script {
                    def revision = checkout([$class: 'GitSCM',
                        branches: [[name: "*/${params.SOURCE_BRANCH}"]],
                        userRemoteConfigs: scm.userRemoteConfigs,
                        extensions: [[$class: 'PruneStaleBranch']]])
                    env.RELEASE_VERSION = "1.0.${env.BUILD_NUMBER}+${revision.GIT_COMMIT}"
                    currentBuild.description = "${params.SOURCE_BRANCH} · ${revision.GIT_COMMIT.take(8)}"
                }
            }
        }
        stage('Restore') {
            steps { powershell '. .\\scripts\\Common.ps1; Invoke-DotNet restore WebBanHang.sln --locked-mode' }
        }
        stage('Build') {
            steps { powershell '. .\\scripts\\Common.ps1; Invoke-DotNet build WebBanHang.sln -c Release --no-restore "-p:InformationalVersion=$env:RELEASE_VERSION" -p:IncludeSourceRevisionInInformationalVersion=false' }
        }
        stage('Test') {
            steps { powershell '. .\\scripts\\Common.ps1; Invoke-DotNet test WebBanHang.sln -c Release --no-build --logger "trx;LogFileName=tests.trx" --results-directory artifacts/TestResults' }
            post { always { archiveArtifacts artifacts: 'artifacts/TestResults/*.trx', allowEmptyArchive: true } }
        }
        stage('Publish') {
            steps { powershell '. .\\scripts\\Common.ps1; Invoke-DotNet publish src/WebBanHang/WebBanHang.csproj -c Release --no-build -o artifacts/publish' }
        }
        stage('Deploy') {
            when { expression { params.SOURCE_BRANCH == 'main' && params.DEPLOY_TO_IIS } }
            steps { powershell '& .\\scripts\\Deploy-IIS.ps1 -PublishPath artifacts/publish -DeployPath $env:DEPLOY_PATH -VerifyUrl $env:VERIFY_URL -ExpectedVersion $env:RELEASE_VERSION' }
        }
        stage('Verify') {
            when { expression { params.SOURCE_BRANCH == 'main' && params.DEPLOY_TO_IIS } }
            steps { powershell '& .\\scripts\\Verify-Deployment.ps1 -Url $env:VERIFY_URL -ExpectedVersion $env:RELEASE_VERSION' }
        }
    }
    post {
        success { echo 'Build and tests passed. Check Deploy/Verify stage status before claiming IIS deployment.' }
        failure { echo 'Pipeline failed. Build/Test failures never reach Publish/Deploy. Deployment failures attempt rollback; inspect logs.' }
    }
}
