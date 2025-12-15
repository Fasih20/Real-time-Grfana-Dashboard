'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { Layout, Button, Typography, Spin, message, Badge } from 'antd';
import { EditOutlined, LogoutOutlined, DashboardOutlined, SafetyCertificateOutlined } from '@ant-design/icons';

const { Header, Content } = Layout;
const { Title } = Typography;

export default function Dashboard() {
  const router = useRouter();
  const [role, setRole] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  // REPLACE THIS WITH YOUR LOCAL GRAFANA URL
  // The "&kiosk" part is important—it hides the Grafana sidebars
  const GRAFANA_EMBED_URL = "http://localhost:3000/goto/ef76252p9w5c0a?orgId=1&kiosk=tv";

  useEffect(() => {
    // 1. Verify Authentication
    const token = localStorage.getItem('token');
    const storedRole = localStorage.getItem('role'); // "Admin" or "Operator"

    if (!token) {
      message.error('Session expired. Please log in.');
      router.push('/login');
      return;
    }

    setRole(storedRole);
    setLoading(false);
  }, [router]);

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    message.success('Logged out successfully');
    router.push('/login');
  };

  const handleEditClick = () => {
    // 2. The "Goes Nowhere" Button Logic
    message.warning('Edit Mode is currently disabled in this environment.');
  };

  if (loading) {
    return (
      <div className="flex h-screen items-center justify-center bg-gray-50">
        <Spin size="large" tip="Loading Console..." />
      </div>
    );
  }

  return (
    <Layout className="min-h-screen">
      {/* Top Navigation Bar */}
      <Header className="bg-white border-b border-gray-200 px-6 flex items-center justify-between shadow-sm z-10">
        <div className="flex items-center gap-3">
          <DashboardOutlined style={{ fontSize: '24px', color: '#1890ff' }} />
          <div>
            <Title level={4} style={{ margin: 0, lineHeight: '1.2' }}>
              SUPARCO Console
            </Title>
            <span className="text-xs text-gray-400 uppercase tracking-wider">
              Live Monitoring System
            </span>
          </div>
        </div>

        <div className="flex items-center gap-4">
          {/* User Badge */}
          <div className="hidden sm:flex items-center gap-2 px-3 py-1 bg-gray-100 rounded-full">
            <SafetyCertificateOutlined className="text-green-600" />
            <span className="text-sm font-medium text-gray-600">
              {role} Access
            </span>
          </div>
          
          {/* CONDITIONAL BUTTON: Only visible if role is 'Admin' */}
          {role === 'Admin' && (
            <Button 
              type="primary" 
              icon={<EditOutlined />} 
              onClick={handleEditClick}
              className="bg-blue-600"
            >
              Edit Layout
            </Button>
          )}
          
          <Button 
            danger 
            type="text"
            icon={<LogoutOutlined />} 
            onClick={handleLogout}
          >
            Logout
          </Button>
        </div>
      </Header>

      {/* Main Content Area (The Iframe) */}
      <Content className="flex flex-col h-[calc(100vh-64px)] bg-gray-100 p-4">
        <div className="flex-grow bg-white rounded-xl shadow-inner border border-gray-200 overflow-hidden relative">
          {/* The Dashboard Embed */}
          <iframe
            src={GRAFANA_EMBED_URL}
            width="100%"
            height="100%"
            frameBorder="0"
            className="absolute inset-0 w-full h-full"
            title="Grafana Dashboard"
          />
        </div>
      </Content>
    </Layout>
  );
}