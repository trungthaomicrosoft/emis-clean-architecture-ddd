# Deployment Architecture - Kubernetes On-Premise

## 🎯 Deployment Overview

### Infrastructure Requirements

#### Hardware Requirements (Production Cluster)
**Master Nodes (3 nodes for HA):**
- CPU: 8 cores
- RAM: 16 GB
- Disk: 200 GB SSD
- Role: Control plane, etcd

**Worker Nodes (Initial: 6 nodes, Scalable):**
- CPU: 16 cores
- RAM: 32 GB
- Disk: 500 GB SSD
- Role: Run application workloads

**Storage Nodes (3 nodes for distributed storage):**
- CPU: 8 cores
- RAM: 16 GB
- Disk: 2 TB HDD + 200 GB SSD (cache)
- Role: MinIO distributed storage

**Database Nodes:**
- MySQL Cluster: 3 nodes (Primary + 2 Replicas)
  - CPU: 16 cores
  - RAM: 64 GB
  - Disk: 1 TB SSD
- MongoDB Cluster: 3 nodes (Replica Set)
  - CPU: 16 cores
  - RAM: 32 GB
  - Disk: 1 TB SSD
- Redis Cluster: 3 nodes
  - CPU: 8 cores
  - RAM: 32 GB
  - Disk: 200 GB SSD

**Load Balancer:**
- 2 nodes (HA)
- CPU: 4 cores
- RAM: 8 GB
- Software: HAProxy / Nginx

**Total Estimated:**
- **18 Kubernetes nodes** (3 master + 6 worker + 9 infrastructure)
- **Physical servers can be virtualized (VMware, Proxmox, KVM)**

---

## 🏗️ Kubernetes Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Load Balancer (HAProxy)                   │
│                    Public IP: 203.0.113.10                       │
└────────────────────────┬────────────────────────────────────────┘
                         │
        ┌────────────────┼────────────────┐
        │                │                │
┌───────▼──────┐  ┌──────▼─────┐  ┌──────▼──────┐
│  Master 1    │  │  Master 2  │  │  Master 3   │
│  (Control    │  │  (Control  │  │  (Control   │
│   Plane)     │  │   Plane)   │  │   Plane)    │
└──────────────┘  └────────────┘  └─────────────┘
        │                │                │
        └────────────────┼────────────────┘
                         │
        ┌────────────────┼────────────────┬────────────────┐
        │                │                │                │
┌───────▼──────┐  ┌──────▼─────┐  ┌──────▼─────┐  ┌──────▼─────┐
│  Worker 1    │  │  Worker 2  │  │  Worker 3  │  │  Worker 4  │
│  (App Pods)  │  │  (App Pods)│  │  (App Pods)│  │  (App Pods)│
└──────────────┘  └────────────┘  └────────────┘  └────────────┘
        │                │                │                │
        └────────────────┼────────────────┴────────────────┘
                         │
        ┌────────────────┼────────────────┬────────────────┐
        │                │                │                │
┌───────▼──────┐  ┌──────▼─────┐  ┌──────▼─────┐
│  MySQL       │  │  MongoDB   │  │  Redis     │
│  Cluster     │  │  Cluster   │  │  Cluster   │
└──────────────┘  └────────────┘  └────────────┘
```

---

## 📦 Kubernetes Cluster Setup

### 1. Cluster Initialization (kubeadm)

**Master Node 1 (Primary):**
```bash
# Initialize cluster
sudo kubeadm init \
  --control-plane-endpoint="k8s-master-lb.emis.local:6443" \
  --upload-certs \
  --pod-network-cidr=10.244.0.0/16

# Setup kubectl
mkdir -p $HOME/.kube
sudo cp -i /etc/kubernetes/admin.conf $HOME/.kube/config
sudo chown $(id -u):$(id -g) $HOME/.kube/config

# Install CNI (Calico)
kubectl apply -f https://docs.projectcalico.org/manifests/calico.yaml
```

**Master Nodes 2 & 3:**
```bash
# Join as control plane
sudo kubeadm join k8s-master-lb.emis.local:6443 \
  --token <token> \
  --discovery-token-ca-cert-hash sha256:<hash> \
  --control-plane \
  --certificate-key <cert-key>
```

**Worker Nodes:**
```bash
# Join as worker
sudo kubeadm join k8s-master-lb.emis.local:6443 \
  --token <token> \
  --discovery-token-ca-cert-hash sha256:<hash>
```

---

## 🗂️ Namespace Structure

```yaml
# Create namespaces
kubectl create namespace emis-infrastructure
kubectl create namespace emis-services
kubectl create namespace emis-data
kubectl create namespace emis-monitoring
kubectl create namespace emis-ingress
```

**Namespace Organization:**
- `emis-infrastructure`: API Gateway, Service Mesh
- `emis-services`: All microservices
- `emis-data`: Databases, Redis, RabbitMQ, MinIO
- `emis-monitoring`: Prometheus, Grafana, ELK
- `emis-ingress`: Ingress controllers

---

## 🚪 Ingress Configuration

### Install Nginx Ingress Controller
```bash
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.9.4/deploy/static/provider/baremetal/deploy.yaml
```

### Ingress Resource Example
```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: emis-api-ingress
  namespace: emis-infrastructure
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - api.emis.local
    secretName: emis-tls-secret
  rules:
  - host: api.emis.local
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: api-gateway
            port:
              number: 80
```

---

## 🔐 Secrets Management

### Create Secrets
```bash
# Database connection strings
kubectl create secret generic mysql-secret \
  --from-literal=connection-string='Server=mysql.emis-data.svc.cluster.local;Database=emis;User=root;Password=SecurePassword123!' \
  -n emis-services

# JWT signing key
kubectl create secret generic jwt-secret \
  --from-literal=signing-key='your-very-secure-secret-key-min-32-chars' \
  -n emis-services

# MinIO credentials
kubectl create secret generic minio-secret \
  --from-literal=access-key='admin' \
  --from-literal=secret-key='SecurePassword123!' \
  -n emis-data
```

### Using Sealed Secrets (Recommended)
```bash
# Install Sealed Secrets controller
kubectl apply -f https://github.com/bitnami-labs/sealed-secrets/releases/download/v0.24.0/controller.yaml

# Create sealed secret
kubeseal --format=yaml < secret.yaml > sealed-secret.yaml
kubectl apply -f sealed-secret.yaml
```

---

## 🗄️ Database Deployment

### MySQL StatefulSet
```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: mysql
  namespace: emis-data
spec:
  serviceName: mysql
  replicas: 3
  selector:
    matchLabels:
      app: mysql
  template:
    metadata:
      labels:
        app: mysql
    spec:
      containers:
      - name: mysql
        image: mysql:8.0
        ports:
        - containerPort: 3306
          name: mysql
        env:
        - name: MYSQL_ROOT_PASSWORD
          valueFrom:
            secretKeyRef:
              name: mysql-secret
              key: root-password
        volumeMounts:
        - name: mysql-data
          mountPath: /var/lib/mysql
        - name: mysql-config
          mountPath: /etc/mysql/conf.d
        resources:
          requests:
            memory: "4Gi"
            cpu: "2"
          limits:
            memory: "8Gi"
            cpu: "4"
  volumeClaimTemplates:
  - metadata:
      name: mysql-data
    spec:
      accessModes: [ "ReadWriteOnce" ]
      storageClassName: local-storage
      resources:
        requests:
          storage: 500Gi
---
apiVersion: v1
kind: Service
metadata:
  name: mysql
  namespace: emis-data
spec:
  clusterIP: None
  selector:
    app: mysql
  ports:
  - port: 3306
    targetPort: 3306
```

### MongoDB StatefulSet
```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: mongodb
  namespace: emis-data
spec:
  serviceName: mongodb
  replicas: 3
  selector:
    matchLabels:
      app: mongodb
  template:
    metadata:
      labels:
        app: mongodb
    spec:
      containers:
      - name: mongodb
        image: mongo:6.0
        ports:
        - containerPort: 27017
        env:
        - name: MONGO_INITDB_ROOT_USERNAME
          value: "admin"
        - name: MONGO_INITDB_ROOT_PASSWORD
          valueFrom:
            secretKeyRef:
              name: mongodb-secret
              key: root-password
        volumeMounts:
        - name: mongodb-data
          mountPath: /data/db
        resources:
          requests:
            memory: "4Gi"
            cpu: "2"
          limits:
            memory: "8Gi"
            cpu: "4"
  volumeClaimTemplates:
  - metadata:
      name: mongodb-data
    spec:
      accessModes: [ "ReadWriteOnce" ]
      storageClassName: local-storage
      resources:
        requests:
          storage: 500Gi
```

### Redis Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: redis
  namespace: emis-data
spec:
  replicas: 1
  selector:
    matchLabels:
      app: redis
  template:
    metadata:
      labels:
        app: redis
    spec:
      containers:
      - name: redis
        image: redis:7-alpine
        ports:
        - containerPort: 6379
        command:
        - redis-server
        - --requirepass
        - $(REDIS_PASSWORD)
        env:
        - name: REDIS_PASSWORD
          valueFrom:
            secretKeyRef:
              name: redis-secret
              key: password
        resources:
          requests:
            memory: "2Gi"
            cpu: "1"
          limits:
            memory: "4Gi"
            cpu: "2"
        volumeMounts:
        - name: redis-data
          mountPath: /data
      volumes:
      - name: redis-data
        persistentVolumeClaim:
          claimName: redis-pvc
```

---

## 📨 RabbitMQ Deployment

```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: rabbitmq
  namespace: emis-data
spec:
  serviceName: rabbitmq
  replicas: 3
  selector:
    matchLabels:
      app: rabbitmq
  template:
    metadata:
      labels:
        app: rabbitmq
    spec:
      containers:
      - name: rabbitmq
        image: rabbitmq:3.12-management
        ports:
        - containerPort: 5672
          name: amqp
        - containerPort: 15672
          name: management
        env:
        - name: RABBITMQ_DEFAULT_USER
          value: "admin"
        - name: RABBITMQ_DEFAULT_PASS
          valueFrom:
            secretKeyRef:
              name: rabbitmq-secret
              key: password
        - name: RABBITMQ_ERLANG_COOKIE
          value: "secretcookie"
        volumeMounts:
        - name: rabbitmq-data
          mountPath: /var/lib/rabbitmq
        resources:
          requests:
            memory: "2Gi"
            cpu: "1"
          limits:
            memory: "4Gi"
            cpu: "2"
  volumeClaimTemplates:
  - metadata:
      name: rabbitmq-data
    spec:
      accessModes: [ "ReadWriteOnce" ]
      storageClassName: local-storage
      resources:
        requests:
          storage: 50Gi
```

---

## 📦 MinIO Deployment

```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: minio
  namespace: emis-data
spec:
  serviceName: minio
  replicas: 4
  selector:
    matchLabels:
      app: minio
  template:
    metadata:
      labels:
        app: minio
    spec:
      containers:
      - name: minio
        image: minio/minio:latest
        args:
        - server
        - http://minio-{0...3}.minio.emis-data.svc.cluster.local/data
        - --console-address
        - ":9001"
        ports:
        - containerPort: 9000
          name: api
        - containerPort: 9001
          name: console
        env:
        - name: MINIO_ROOT_USER
          valueFrom:
            secretKeyRef:
              name: minio-secret
              key: access-key
        - name: MINIO_ROOT_PASSWORD
          valueFrom:
            secretKeyRef:
              name: minio-secret
              key: secret-key
        volumeMounts:
        - name: minio-data
          mountPath: /data
        resources:
          requests:
            memory: "4Gi"
            cpu: "2"
          limits:
            memory: "8Gi"
            cpu: "4"
  volumeClaimTemplates:
  - metadata:
      name: minio-data
    spec:
      accessModes: [ "ReadWriteOnce" ]
      storageClassName: local-storage
      resources:
        requests:
          storage: 1Ti
```

---

## 🚀 Microservice Deployment Example (Student Service)

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: student-service
  namespace: emis-services
  labels:
    app: student-service
    version: v1
spec:
  replicas: 3
  selector:
    matchLabels:
      app: student-service
  template:
    metadata:
      labels:
        app: student-service
        version: v1
    spec:
      containers:
      - name: student-service
        image: registry.emis.local/student-service:1.0.0
        imagePullPolicy: IfNotPresent
        ports:
        - containerPort: 80
          name: http
        - containerPort: 443
          name: https
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:80"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: mysql-secret
              key: connection-string
        - name: Redis__ConnectionString
          value: "redis.emis-data.svc.cluster.local:6379,password=$(REDIS_PASSWORD)"
        - name: Redis__Password
          valueFrom:
            secretKeyRef:
              name: redis-secret
              key: password
        - name: RabbitMQ__Host
          value: "rabbitmq.emis-data.svc.cluster.local"
        - name: RabbitMQ__Username
          value: "admin"
        - name: RabbitMQ__Password
          valueFrom:
            secretKeyRef:
              name: rabbitmq-secret
              key: password
        - name: JWT__SecretKey
          valueFrom:
            secretKeyRef:
              name: jwt-secret
              key: signing-key
        - name: MinIO__Endpoint
          value: "minio.emis-data.svc.cluster.local:9000"
        - name: MinIO__AccessKey
          valueFrom:
            secretKeyRef:
              name: minio-secret
              key: access-key
        - name: MinIO__SecretKey
          valueFrom:
            secretKeyRef:
              name: minio-secret
              key: secret-key
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 3
      imagePullSecrets:
      - name: registry-secret
---
apiVersion: v1
kind: Service
metadata:
  name: student-service
  namespace: emis-services
spec:
  selector:
    app: student-service
  ports:
  - port: 80
    targetPort: 80
    protocol: TCP
    name: http
  type: ClusterIP
---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: student-service-hpa
  namespace: emis-services
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: student-service
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

---

## 🌐 API Gateway Deployment (YARP)

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api-gateway
  namespace: emis-infrastructure
spec:
  replicas: 3
  selector:
    matchLabels:
      app: api-gateway
  template:
    metadata:
      labels:
        app: api-gateway
    spec:
      containers:
      - name: api-gateway
        image: registry.emis.local/api-gateway:1.0.0
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
---
apiVersion: v1
kind: Service
metadata:
  name: api-gateway
  namespace: emis-infrastructure
spec:
  selector:
    app: api-gateway
  ports:
  - port: 80
    targetPort: 80
  type: LoadBalancer
```

---

## 📊 Monitoring Stack

### Prometheus Deployment
```bash
# Add Prometheus Helm repo
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

# Install Prometheus
helm install prometheus prometheus-community/kube-prometheus-stack \
  --namespace emis-monitoring \
  --create-namespace \
  --set prometheus.prometheusSpec.retention=30d \
  --set prometheus.prometheusSpec.storageSpec.volumeClaimTemplate.spec.resources.requests.storage=100Gi
```

### Grafana Dashboards
- Kubernetes cluster metrics
- Application metrics
- Database metrics
- RabbitMQ metrics
- Custom EMIS dashboards

### ELK Stack Deployment
```bash
# Add Elastic Helm repo
helm repo add elastic https://helm.elastic.co
helm repo update

# Install Elasticsearch
helm install elasticsearch elastic/elasticsearch \
  --namespace emis-monitoring \
  --set replicas=3 \
  --set volumeClaimTemplate.resources.requests.storage=200Gi

# Install Kibana
helm install kibana elastic/kibana \
  --namespace emis-monitoring \
  --set elasticsearchHosts="http://elasticsearch-master:9200"

# Install Filebeat (log shipper)
helm install filebeat elastic/filebeat \
  --namespace emis-monitoring
```

---

## 🔄 Backup & Disaster Recovery

### Database Backup Strategy

**MySQL Backup CronJob:**
```yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: mysql-backup
  namespace: emis-data
spec:
  schedule: "0 2 * * *"  # Daily at 2 AM
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: mysql-backup
            image: mysql:8.0
            command:
            - /bin/sh
            - -c
            - |
              mysqldump -h mysql.emis-data.svc.cluster.local \
                -u root -p$MYSQL_ROOT_PASSWORD \
                --all-databases --single-transaction \
                | gzip > /backup/mysql-$(date +%Y%m%d-%H%M%S).sql.gz
              # Upload to remote backup storage
              aws s3 cp /backup/mysql-*.sql.gz s3://emis-backups/mysql/
            env:
            - name: MYSQL_ROOT_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: mysql-secret
                  key: root-password
            volumeMounts:
            - name: backup
              mountPath: /backup
          restartPolicy: OnFailure
          volumes:
          - name: backup
            persistentVolumeClaim:
              claimName: backup-pvc
```

**MongoDB Backup CronJob:**
```yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: mongodb-backup
  namespace: emis-data
spec:
  schedule: "0 3 * * *"  # Daily at 3 AM
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: mongodb-backup
            image: mongo:6.0
            command:
            - /bin/sh
            - -c
            - |
              mongodump --host mongodb-0.mongodb.emis-data.svc.cluster.local \
                --username admin --password $MONGO_PASSWORD \
                --out /backup/mongodb-$(date +%Y%m%d-%H%M%S) \
                --gzip
              # Upload to remote backup storage
            env:
            - name: MONGO_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: mongodb-secret
                  key: root-password
            volumeMounts:
            - name: backup
              mountPath: /backup
          restartPolicy: OnFailure
          volumes:
          - name: backup
            persistentVolumeClaim:
              claimName: backup-pvc
```

### Velero for Cluster Backup
```bash
# Install Velero
velero install \
  --provider aws \
  --plugins velero/velero-plugin-for-aws:v1.8.0 \
  --bucket emis-k8s-backups \
  --backup-location-config region=us-east-1 \
  --snapshot-location-config region=us-east-1 \
  --secret-file ./credentials-velero

# Schedule daily backup
velero schedule create daily-backup \
  --schedule="0 1 * * *" \
  --include-namespaces emis-services,emis-data
```

---

## 🔐 SSL/TLS Configuration

### Cert-Manager for Automated Certificates
```bash
# Install cert-manager
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml

# Create ClusterIssuer
kubectl apply -f - <<EOF
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: admin@emis.com
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
EOF
```

---

## 📈 Scaling Strategy

### Horizontal Pod Autoscaling (HPA)
- CPU-based: 70% threshold
- Memory-based: 80% threshold
- Custom metrics: Request rate, Queue length

### Cluster Autoscaling
- Add worker nodes when resource pressure
- Min nodes: 6
- Max nodes: 20

### Database Scaling
- **Read Replicas:** Add MySQL read replicas for read-heavy workloads
- **Sharding:** MongoDB sharding for horizontal scaling
- **Connection Pooling:** Optimize connection usage

---

## 🚦 Traffic Management

### Service Mesh (Optional: Istio)
```bash
# Install Istio
istioctl install --set profile=production -y

# Enable sidecar injection
kubectl label namespace emis-services istio-injection=enabled
```

**Features:**
- Traffic routing
- Load balancing
- Circuit breaking
- Observability
- Security (mTLS)

---

## 📋 Deployment Checklist

### Pre-Deployment
- [ ] Kubernetes cluster ready (3 masters, 6+ workers)
- [ ] Storage provisioner configured
- [ ] Network policies defined
- [ ] Secrets created
- [ ] Docker registry accessible
- [ ] DNS configured
- [ ] Load balancer setup

### Infrastructure Deployment
- [ ] Deploy databases (MySQL, MongoDB, Redis)
- [ ] Deploy message bus (RabbitMQ)
- [ ] Deploy file storage (MinIO)
- [ ] Verify infrastructure health

### Application Deployment
- [ ] Deploy API Gateway
- [ ] Deploy Identity Service
- [ ] Deploy microservices (Student, Teacher, etc.)
- [ ] Configure ingress
- [ ] Setup SSL/TLS

### Monitoring & Observability
- [ ] Deploy Prometheus & Grafana
- [ ] Deploy ELK Stack
- [ ] Configure dashboards
- [ ] Setup alerts

### Security
- [ ] Network policies applied
- [ ] RBAC configured
- [ ] Secrets encrypted
- [ ] Security scanning enabled

### Backup & DR
- [ ] Backup CronJobs configured
- [ ] Velero backup scheduled
- [ ] Disaster recovery plan documented
- [ ] Restore procedure tested

---

## 🔧 Operational Commands

### Check Service Status
```bash
# All services
kubectl get all -n emis-services

# Specific service
kubectl get pods -n emis-services -l app=student-service

# Logs
kubectl logs -n emis-services -l app=student-service --tail=100 -f

# Describe pod
kubectl describe pod -n emis-services <pod-name>
```

### Scaling
```bash
# Manual scale
kubectl scale deployment student-service -n emis-services --replicas=5

# Check HPA status
kubectl get hpa -n emis-services
```

### Rolling Update
```bash
# Update image
kubectl set image deployment/student-service \
  student-service=registry.emis.local/student-service:1.0.1 \
  -n emis-services

# Check rollout status
kubectl rollout status deployment/student-service -n emis-services

# Rollback
kubectl rollout undo deployment/student-service -n emis-services
```

### Debugging
```bash
# Execute command in pod
kubectl exec -it <pod-name> -n emis-services -- /bin/bash

# Port forward
kubectl port-forward svc/student-service 8080:80 -n emis-services

# Check events
kubectl get events -n emis-services --sort-by='.lastTimestamp'
```

---

## 📊 Cost Estimation (On-Premise)

### Initial Hardware Investment
- **18 physical servers** (avg $3,000 each): $54,000
- **Networking equipment**: $10,000
- **Storage infrastructure**: $15,000
- **Backup systems**: $8,000
- **Total Initial**: ~$87,000

### Annual Operational Costs
- **Electricity** (~100 kW): $15,000/year
- **Cooling**: $8,000/year
- **Internet bandwidth**: $6,000/year
- **Maintenance & Support**: $12,000/year
- **Total Annual**: ~$41,000/year

### Staffing
- **DevOps Engineers** (2): $100,000/year
- **System Administrators** (2): $80,000/year

---

## 🎓 Maintenance & Operations

### Daily Tasks
- Monitor system health
- Check backup status
- Review alerts
- Performance monitoring

### Weekly Tasks
- Security updates
- Performance optimization
- Capacity planning review
- Incident review

### Monthly Tasks
- Backup restore testing
- Security audit
- Cost analysis
- Documentation updates

---

## 📚 Additional Resources

- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [Helm Charts](https://helm.sh/)
- [CNCF Landscape](https://landscape.cncf.io/)
- [Kubernetes Best Practices](https://kubernetes.io/docs/concepts/configuration/overview/)

---

**Previous:** [05-Technology-Stack.md](./05-Technology-Stack.md)  
**Back to:** [01-System-Overview.md](./01-System-Overview.md)
