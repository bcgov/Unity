# Instructions to Install Unity SSL Certificate

## Step 1: Submit a CSR

A Certificate Signing Request (CSR) is necessary for new certificates or certificate renewals. Contact the ISB Operations team they will make an iStore request with associated approved funding and generate the required .csr file, then provide the SSL certificate files when they are ready.


## Step 2: Install SSL certificates

As a best practice, store copies of these files in the ISB Operations SSL certificate store (e.g. Zone-B server filesystem). That way, the keys can be retrieved when needed. Only project namespace administrators can edit OpenShift certificate objects.

Ensure you have all four (4) required files:

- Certificate: unity.gov.bc.ca.txt
- Private Key: unity.gov.bc.ca.key
- CA Certificate: L1KChain.txt
- CA Root Certificate: G2Root.txt

## Step 3: Create route for unity.gov.bc.ca

You can create network routes using the web OpenShift console or the oc CLI.

Using the command line, the following example creates a secured HTTPS route named `unity-gov-bc-ca` that directs traffic to the `unity-grantmanager-web` service:

```bash
oc create route edge unity-gov-bc-ca \
 --service=unity-grantmanager-web \
 --cert=unity.gov.bc.ca.txt \
 --key=unity.gov.bc.ca.key \
 --ca-cert=L1KChain.txt \
 --hostname=unity.gov.bc.ca \
 --insecure-policy=Redirect
```

Using the web console, you can navigate to the **Administrator > Networking > Routes** section of the conaole.

Click **Create Route** to define and create a route in the project.

Use the following settings:

- Name: unity-gov-bc-ca
- Hostname: unity.gov.bc.ca
- Path: `/`
- Service: unity-grantmanager-web
- Secure Route: (yes)
- TLS Termination: Edge
- Insecure Traffic: Redirect

| Route field                |  Source file        |
| -------------------------- | ------------------- |
| Certificate                |  unity.gov.bc.ca.txt |
| Private Key                |  unity.gov.bc.ca.key |
| CA Certificate             |  L1KChain.txt       |

## Step 4: Verify new route

The site should work immediately after saving these route settings.

- Check that https://unity.gov.bc.ca is live and that the application landing page loads correctly.
- Verify SSO (Keycloak) settings - https://bcgov.github.io/sso-requests

## Optional steps to generate a local CSR
Run the openssl utility with the CSR and private key options **these do not need to be created on the intended machine or containers**. 

```bashs
openssl req -new -newkey rsa:2048 -nodes -out unity.gov.bc.ca.csr \
  -keyout unity.gov.bc.ca.key \
  -subj "/C=CA/ST=British Columbia/L=Victoria/O=Government of the Province of British Columbia/OU=CITZ/CN=unity.gov.bc.ca"
```

Response should be:

```
Generating a RSA private key
.........+++++
...............................+++++
writing new private key to 'unity.gov.bc.ca.key'
-----
You are about to be asked to enter information that will be incorporated
into your certificate request.
What you are about to enter is what is called a Distinguished Name or a DN.
There are quite a few fields but you can leave some blank
For some fields there will be a default value,
If you enter '.', the field will be left blank.
-----
Country Name (2 letter code) [AU]:CA
State or Province Name (full name) [Some-State]:British Columbia
Locality Name (eg, city) []:Victoria
Organization Name (eg, company) [Internet Widgits Pty Ltd]:Government of the Province of British Columbia
Organizational Unit Name (eg, section) []:JEDI
Common Name (e.g. server FQDN or YOUR name) []:unity.gov.bc.ca
Email Address []:

Keep the secret key and send the `.csr` file to the OCIO Access and Directory Management Services team they will require an iStore order to process the `.csr` file and will provide the SSL certificates when they are ready.