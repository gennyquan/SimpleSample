pipeline {
    agent none
    options {
        parallelsAlwaysFailFast() 
    }
    stages {
        stage('Multiple Builds Run') {
            parallel {
                stage('Windows Build') {
                    agent { label 'PhysicLap-UnityWindows' }
                    stages{
                        stage('Verify WIX'){
                            steps {
                                bat '''
                                dotnet tool list -g
                                dotnet tool list --global | findstr /i "wix"
                                if errorlevel 1 (
                                    echo "WiX .NET tool is NOT installed"
                                    exit /b 1
                                ) else (
                                    echo "WiX .NET tool is installed"
                                )
                                '''
                            }
                        }
                        stage('Clone Repository') {
                            steps {
                                echo "Killing Unity Licensing Client."
                                script {
                                    def exitCode = bat(script: 'taskkill /F /IM Unity.Licensing.Client.exe', returnStatus: true)

                                    if (exitCode != 0) {
                                        echo "Unity Licensing Client was not running."
                                    }
                                }

                                echo "Cleaning WORKSPACE."
                                cleanWs() 
                                echo "WORKSPACE cleaned. Pulling from repo"
                                checkout scm
                                echo "Pulled from repo"
                                
                            }
                        }
                        stage('Prepare version'){
                            steps{
                                script {
                                    echo "Preparing version"
                                    def tag = bat(script: '@git describe --tags --abbrev=0', returnStdout: true).trim()
                                    if (!tag) {
                                        error "No git tag found in repository."
                                    }
                                    env.BUILD_VERSION = tag.replaceFirst(/^v/, '')
                                    echo "Version: ${env.BUILD_VERSION}"
                                    echo """Locating Path:${env.WORKSPACE}"""
                                    def filePath="""${env.WORKSPACE}\\MSIInstaller\\SimpleSample.wxs"""
                                    echo """Locating File:${filePath}"""
                                    if (!fileExists(filePath)) {
                                        error "Target file not found: ${filePath}"
                                    }
                                }
                            }
                        }
                        stage('Build') {
                            steps {
                                bat """
                                    "${UnityStudioPath}Unity.exe" ^
                                    -quit -batchmode ^
                                    -projectPath "${env.WORKSPACE}" ^
                                    -logFile - ^
                                    -buildWindows64Player "${env.WORKSPACE}\\release\\SimpleSample.exe"
                                """
                            }
                            post {
                                always {
                                    script {
                                        if (fileExists("""${env.WORKSPACE}\\release\\SimpleSample.exe""")) {
                                            echo "Success: SimpleSample.exe was found at: ${env.WORKSPACE}\\release\\SimpleSample.exe"
                                        } else {
                                            error "Cannot located build artifact"
                                        }
                                    }
                                }
                            }
                        }
                        stage("Bundle"){
                            steps {
                                script {
                                    echo "WXS File: ${env.WORKSPACE}\\MSIInstaller\\SimpleSample.wxs"
                                    echo "Output File: ${env.WORKSPACE}\\installer\\SimpleSample.msi"
                                    echo "PACKAGEVERSION: ${env.BUILD_VERSION}"
                                    echo "SOURCEFILES: ${env.WORKSPACE}\\release"
                                    bat """
                                        wix eula accept wix7
                                        wix ^
                                        build -pdb none -nologo "${env.WORKSPACE}\\MSIInstaller\\SimpleSample.wxs" ^
                                        -out "${env.WORKSPACE}\\installer\\SimpleSample_${env.BUILD_VERSION}.msi" ^
                                        -d PACKAGEVERSION="${env.BUILD_VERSION}" ^
                                        -d RELEASEPATH="${env.WORKSPACE}\\release"
                                    """
                                    if (fileExists("""${env.WORKSPACE}\\installer\\SimpleSample_${env.BUILD_VERSION}.msi""")) {
                                        echo "Success: SimpleSample_${env.BUILD_VERSION}.msi was found at: ${env.WORKSPACE}\\installer\\SimpleSample_${env.BUILD_VERSION}.msi"
                                    } else {
                                        error "Cannot located build artifact"
                                    }
                                    
                                }
                            }
                        }
                        stage("Sign"){
                            steps {
                                script {
                                    bat """
                                    signtool sign /fd SHA256 /tr http://timestamp.digicert.com ^
                                    /td SHA256 /f "${env.CERT_PATH}" /p "${env.CERT_PASSWORD}" ^
                                    "${env.WORKSPACE}\\installer\\SimpleSample_${env.BUILD_VERSION}.msi"
                                    """
                                }
                            }
                        }
                    }
                    post {
                        success {
                            archiveArtifacts artifacts: 'installer/*.msi', onlyIfSuccessful: true
                        }
                    }
                }
            }            
        }
    }
}
