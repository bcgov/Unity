# Instructions to Install Unity Project

## Step 1: Create templates from code

You can create the required templates using the web OpenShift console or the oc CLI.
```
# Delete build templates
oc delete templates --all

# Create build templates
oc create -f $repository\database\unity-backup-cronjob.yaml
oc create -f $repository\database\unity-database.yaml
oc create -f $repository\openshift\unity-imagestream.yaml
oc create -f $repository\openshift\unity-grantmanager-dbmigrator-job.yaml
oc create -f $repository\openshift\unity-grantmanager-web.yaml
oc create -f $repository\openshift\unity-networkpolicy.yaml
oc create -f $repository\openshift\unity-rabbitmq.yaml
oc create -f $repository\openshift\unity-s3-object-storage.yaml
oc create -f $repository\openshift\unity-app-data-web.json
oc create -f $repository\openshift\unity-chefs-data-web.json
oc create -f $repository\openshift\unity-metabase.yaml
```

## Step 2: Create .env paramater files

As a best practice, store copies of these files in a secure location.
```
"database.env"
"dbmigrator-job.env"
"grantmanager-web.env"
"S3-storage.env"
"metabase.env"
"rabbitmq.env"
```

Use oc get templates to find all available parameters of a project template.
oc get templates

| **NAME**  | **DESCRIPTION** |
|-----------|-----------------|
| unity-app-data-web                   | An example Nginx HTTP server and a reverse proxy (nginx) application that serves web content. |
| unity-backup-cronjob                 | Template for running a recurring backup script in OpenShift.                   |
| unity-chefs-data-web                 | An example Nginx HTTP server and a reverse proxy (nginx) application that serves web content. |
| unity-database                       | PostgreSQL database service with persistent storage.                           |
| unity-grantmanager-dbmigrator-job    | Template for running a dotnet console application once in OpenShift.           |
| unity-grantmanager-pgbackup-job      | Template for running a dotnet console application once in OpenShift.           |
| unity-grantmanager-web               | Template for running a DotNet web application on OpenShift.                    |
| unity-imagestream                    | Template for tracking of changes in the application image.                     |
| unity-metabase                       | Template for running a DotNet web application on OpenShift.                    |
| unity-networkpolicy                  | Template for communications rules in OpenShift.                                |
| unity-rabbitmq                       | Template for running RabbitMQ message queue application on OpenShift.          |
| unity-s3-object-storage              | Template for S3 connection information in OpenShift.                           |

## Step 3: Create or replace project resources

You can create OpenShift resources using the web OpenShift console or the oc CLI.

Using the command line,
```
# Replace the running network and namespace policy
oc delete networkpolicies --all
oc process unity-networkpolicy | oc create -f -
oc policy add-role-to-user system:image-puller system:serviceaccount:${project}:default --namespace=${tools}
oc policy add-role-to-group system:image-puller system:serviceaccounts:${project} --namespace=${tools}

# Create Database objects from templates with parameters
oc process unity-database --param-file=${params}-database.env | oc create -f -
helm upgrade --install ${release}-hippo-ha .  -f  $repository\database\crunchy-postgres\values.yaml -f ${params}-pgo-custom-values.yaml
oc process unity-backup-cronjob --param-file=${params}-database.env | oc create -f -

# Create DbMigraitor objects from templates with parameters
oc process unity-grantmanager-imagestream -p APPLICATION_GROUP=${release}-unity-grantmanager -p APPLICATION_NAME=${release}-unity-dbmigrator | oc create -f -
oc import-image ${release}-unity-dbmigrator:$tag --confirm --from=image-registry.openshift-image-registry.svc:5000/${tools}/${release}-unity-dbmigrator-build:$tag
oc process unity-grantmanager-dbmigrator-job --param-file=${params}-dbmigrator-job.env | oc create -f -
oc wait jobs/${release}-unity-dbmigrator --for condition=complete --timeout=120s

# Create S3 storage objects from templates with parameters
oc process unity-s3-object-storage --param-file=${params}-S3.env | oc create -f -

# Create GrantManager objects from templates with parameters
oc process unity-grantmanager-imagestream -p APPLICATION_GROUP=${release}-unity-grantmanager -p APPLICATION_NAME=${release}-unity-grantmanager | oc create -f -
oc import-image ${release}-unity-grantmanager:$tag --confirm --from=image-registry.openshift-image-registry.svc:5000/${tools}/${release}-unity-grantmanager-build:$tag
oc process unity-grantmanager-web --param-file=${params}-grantmanager-web.env | oc create -f -
oc wait dc/${release}-unity-grantmanager-web --for condition=available=true --timeout=120s

# Create RabbitMQ objects from templates with parameters
oc process unity-rabbitmq --param-file=${project}-rabbitmq.env | oc create -f -
oc wait dc/${namespace}unity-rabbitmq --for condition=available

# Deployment for app-data-web
oc process unity-app-data-web -p IMAGEPULL_NAMESPACE=${tools} -p IMAGESTREAM_NAME=${namespace}-unity-app-data-build -p IMAGESTREAM_TAG=latest | oc create -f -

# Deployment for reporting
oc process unity-metabase --param-file=${project}-metabase.env | oc create -f -
```