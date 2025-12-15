'use client';

import { useState } from 'react';
import axios from 'axios';
import { Form, Input, Button, Card, Typography, Alert, message } from 'antd';
import { UserOutlined, LockOutlined } from '@ant-design/icons';
import { useRouter } from 'next/navigation';

const { Title, Text } = Typography;

export default function Login() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  const onFinish = async (values: any) => {
    setLoading(true);
    setErrorMsg('');

    try {
      // 1. Call your C# API
      // Note: We use the exact URL from your launchSettings.json
      const response = await axios.post('http://localhost:5255/api/auth/login', {
        Username: values.username, // Matches UserModel.cs case-insensitive usually, but sending clean JSON
        Password: values.password
      });

      // 2. Handle Success
      const { token, role } = response.data;
      
      // Store the JWT for future requests
      localStorage.setItem('token', token);
      localStorage.setItem('role', role);
      
      message.success(`Welcome back, ${role}!`);
      
      // Redirect to the main dashboard (we will create this next)
      router.push('/dashboard');

    } catch (error: any) {
      console.error('Login error:', error);
      // 3. Handle Errors safely
      const msg = error.response?.data?.message || 'Connection failed. Is the backend running?';
      setErrorMsg(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-100">
      <Card className="w-full max-w-md shadow-xl rounded-lg" bordered={false}>
        <div className="text-center mb-8">
          <Title level={2} style={{ color: '#1890ff' }}>SUPARCO</Title>
          <Text type="secondary">Monitoring & Control Console</Text>
        </div>

        {errorMsg && (
          <Alert message="Authentication Failed" description={errorMsg} type="error" showIcon className="mb-6" />
        )}

        <Form
          name="login_form"
          initialValues={{ remember: true }}
          onFinish={onFinish}
          size="large"
          layout="vertical"
        >
          <Form.Item
            name="username"
            rules={[{ required: true, message: 'Please input your Username!' }]}
          >
            <Input prefix={<UserOutlined />} placeholder="Username" />
          </Form.Item>

          <Form.Item
            name="password"
            rules={[{ required: true, message: 'Please input your Password!' }]}
          >
            <Input.Password prefix={<LockOutlined />} placeholder="Password" />
          </Form.Item>

          <Form.Item>
            <Button type="primary" htmlType="submit" className="w-full bg-blue-600" loading={loading}>
              Log in
            </Button>
          </Form.Item>
        </Form>
        
        <div className="text-center text-xs text-gray-400 mt-4">
          Authorized Personnel Only | System Version 1.0
        </div>
      </Card>
    </div>
  );
}